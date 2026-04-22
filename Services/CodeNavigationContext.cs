#nullable enable
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using CascadeIDE.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services;

/// <summary>
/// Парето-реализация контекста навигации (ADR 0039): режимы <c>related</c> и <c>subgraph</c> для MCP и будущего UI.
/// Источник файлов — дерево решения; семантика — эвристики + partial-классы без полного MSBuildWorkspace.
/// </summary>
public static class CodeNavigationContextBuilder
{
    public const int DefaultMaxRelated = 32;
    public const int DefaultMaxNodes = 12;
    public const int DefaultMaxEdges = 24;

    private static readonly JsonSerializerOptions s_compactJson = new() { WriteIndented = false };

    /// <summary>Общие входы после валидации пресета, якоря и списков файлов дерева.</summary>
    private sealed record PreparedNavigationInputs(
        string Anchor,
        IReadOnlyList<string> AllCs,
        IReadOnlyList<string> NavFiles,
        IReadOnlyList<string> MarkupPaths,
        string? SolutionPath,
        CodeNavigationKindFilter KindFilter,
        string? PresetRequested);

    /// <summary>Снимок путей из дерева решения (UI-поток); тяжёлый <see cref="BuildJson"/> можно вызывать в фоне после этого.</summary>
    public static string BuildJson(
        string mode,
        string? anchorPath,
        string? fallbackCurrentPath,
        ObservableCollection<SolutionItem> roots,
        string? solutionPath,
        int? line,
        int? column,
        int maxRelated,
        int maxNodes,
        int maxEdges,
        IReadOnlyList<string>? includeKinds = null,
        IReadOnlyList<string>? excludeKinds = null,
        string? preset = null,
        CodeNavigationSettings? codeNavigation = null)
    {
        var rawPaths = McpSolutionTree.CollectFileEntries(roots).Select(e => e.FullPath);
        return BuildJson(
            mode,
            anchorPath,
            fallbackCurrentPath,
            rawPaths,
            solutionPath,
            line,
            column,
            maxRelated,
            maxNodes,
            maxEdges,
            includeKinds,
            excludeKinds,
            preset,
            codeNavigation);
    }

    /// <param name="knownFilePathsFromSolution">Пути из дерева (как из <see cref="McpSolutionTree.CollectFileEntries"/>), до нормализации.</param>
    public static string BuildJson(
        string mode,
        string? anchorPath,
        string? fallbackCurrentPath,
        IEnumerable<string> knownFilePathsFromSolution,
        string? solutionPath,
        int? line,
        int? column,
        int maxRelated,
        int maxNodes,
        int maxEdges,
        IReadOnlyList<string>? includeKinds = null,
        IReadOnlyList<string>? excludeKinds = null,
        string? preset = null,
        CodeNavigationSettings? codeNavigation = null)
    {
        if (!TryPrepareNavigationInputs(
                anchorPath,
                fallbackCurrentPath,
                knownFilePathsFromSolution,
                solutionPath,
                includeKinds,
                excludeKinds,
                preset,
                codeNavigation,
                out var prepared,
                out var errorJson))
            return errorJson;

        var m = mode.Trim().ToLowerInvariant();
        return m switch
        {
            "related" => BuildRelated(prepared, maxRelated, line, column),
            "subgraph" => BuildSubgraph(prepared, maxNodes, maxEdges, line, column),
            _ => JsonSerializer.Serialize(new { error = "bad_mode", message = "mode: related | subgraph", mode })
        };
    }

    private static List<string> NormalizeKnownFilePaths(IEnumerable<string> paths) =>
        paths
            .Where(static p => !string.IsNullOrEmpty(p))
            .Select(static p =>
            {
                try
                {
                    return Path.GetFullPath(p);
                }
                catch
                {
                    return (string?)null;
                }
            })
            .Where(static p => p is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool TryPrepareNavigationInputs(
        string? anchorPath,
        string? fallbackCurrentPath,
        IEnumerable<string> knownFilePathsFromSolution,
        string? solutionPath,
        IReadOnlyList<string>? includeKinds,
        IReadOnlyList<string>? excludeKinds,
        string? preset,
        CodeNavigationSettings? codeNavigation,
        out PreparedNavigationInputs prepared,
        out string errorJson)
    {
        prepared = default!;
        errorJson = "";
        var pj = CodeNavigationPresetsLoader.GetEffectivePresetsJson(
            codeNavigation ?? new CodeNavigationSettings(),
            solutionPath);
        var (mergedInc, mergedExc, presetErr) = CodeNavigationPresetMerge.Merge(preset, pj, includeKinds, excludeKinds);
        if (presetErr is not null)
        {
            errorJson = JsonSerializer.Serialize(new { error = "bad_preset", message = presetErr, preset });
            return false;
        }

        var kindFilter = CodeNavigationKindFilter.Create(mergedInc, mergedExc);
        var anchor = !string.IsNullOrWhiteSpace(anchorPath)
            ? anchorPath.Trim()
            : fallbackCurrentPath?.Trim();
        if (string.IsNullOrEmpty(anchor))
        {
            errorJson = JsonSerializer.Serialize(new { error = "no_file", message = "Укажите file_path или откройте файл в редакторе." });
            return false;
        }

        try
        {
            anchor = Path.GetFullPath(anchor);
        }
        catch
        {
            errorJson = JsonSerializer.Serialize(new { error = "bad_path", message = anchor });
            return false;
        }

        var allKnownFiles = NormalizeKnownFilePaths(knownFilePathsFromSolution);

        var known = new HashSet<string>(allKnownFiles, StringComparer.OrdinalIgnoreCase);
        if (!known.Contains(anchor))
        {
            errorJson = JsonSerializer.Serialize(new { error = "file_not_in_solution", message = "Файл не из загруженного решения.", path = anchor });
            return false;
        }

        var navFiles = allKnownFiles.Where(f => !McpSolutionTree.IsBuildArtifactPath(f)).ToList();
        var allCs = navFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
        if (allCs.Count == 0)
        {
            errorJson = JsonSerializer.Serialize(new { error = "no_solution_files", message = "Нет .cs в дереве решения." });
            return false;
        }

        var markupPaths = navFiles
            .Where(f => f.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        prepared = new PreparedNavigationInputs(anchor, allCs, navFiles, markupPaths, solutionPath, kindFilter, preset);
        return true;
    }

    private static string BuildRelated(
        PreparedNavigationInputs ctx,
        int maxRelated,
        int? line,
        int? column)
    {
        var items = CollectRelatedItems(ctx, maxRelated);
        var kindFilterPayload = new
        {
            preset = ctx.PresetRequested,
            include_kinds_effective = ctx.KindFilter.EffectiveIncludeKinds,
            exclude_kinds_effective = ctx.KindFilter.EffectiveExcludeKinds
        };

        var payload = new
        {
            mode = "related",
            anchor_path = ctx.Anchor,
            line,
            column,
            max_related = maxRelated,
            kind_filter = kindFilterPayload,
            items
        };
        return JsonSerializer.Serialize(payload, s_compactJson);
    }

    private static List<object> CollectRelatedItems(PreparedNavigationInputs ctx, int maxRelated)
    {
        var anchor = ctx.Anchor;
        var allCs = ctx.AllCs;
        var allKnownFiles = ctx.NavFiles;
        var markupPaths = ctx.MarkupPaths;
        var solutionPath = ctx.SolutionPath;
        var kindFilter = ctx.KindFilter;

        var owningCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? Owning(string path)
        {
            if (!owningCache.TryGetValue(path, out var o))
            {
                o = McpSolutionTree.ResolveOwningProjectPath(path);
                owningCache[path] = o;
            }
            return o;
        }

        var items = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(anchor) };

        void AddIfNew(string path, string kind, string rationale)
        {
            if (!kindFilter.Allows(kind))
                return;
            if (items.Count >= maxRelated)
                return;
            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }

            if (seen.Contains(full))
                return;
            seen.Add(full);
            items.Add(new
            {
                path = full,
                kind,
                rationale,
                relative_path = McpSolutionTree.GetRelativePath(solutionPath, full)
            });
        }

        var anchorIsCs = anchor.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

        if (anchorIsCs)
        {
            foreach (var name in EnumeratePartialTypeNames(anchor))
            {
                foreach (var peer in FindPartialPeers(allCs, anchor, name))
                {
                    if (items.Count >= maxRelated)
                        goto AfterPartial;
                    AddIfNew(peer, "partial_peer", $"Partial того же типа «{name}»");
                }
            }
        }

        AfterPartial:

        var anchorProj = Owning(anchor);
        if (items.Count < maxRelated && !string.IsNullOrEmpty(anchorProj))
        {
            foreach (var f in allCs
                         .Where(f => !string.Equals(Path.GetFullPath(f), Path.GetFullPath(anchor), StringComparison.OrdinalIgnoreCase))
                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                if (items.Count >= maxRelated)
                    break;
                var fp = Owning(f);
                if (!string.IsNullOrEmpty(fp) && string.Equals(fp, anchorProj, StringComparison.OrdinalIgnoreCase))
                    AddIfNew(f, "project_peer", "Тот же проект");
            }
        }

        if (items.Count < maxRelated)
        {
            foreach (var p in FindXamlCodeBehindPairs(anchor, allCs, markupPaths))
            {
                if (items.Count >= maxRelated)
                    break;
                AddIfNew(p.path, "xaml_codebehind_pair", p.rationale);
            }
        }

        if (items.Count < maxRelated && anchorIsCs)
        {
            foreach (var p in FindTestCounterparts(anchor, allCs))
            {
                if (items.Count >= maxRelated)
                    break;
                AddIfNew(p.path, "test_counterpart", p.rationale);
            }
        }

        if (items.Count < maxRelated && anchorIsCs)
        {
            var anchorNs = ExtractNamespaces(anchor);
            if (anchorNs.Count > 0)
            {
                foreach (var f in allCs
                             .Where(f => !string.Equals(Path.GetFullPath(f), Path.GetFullPath(anchor), StringComparison.OrdinalIgnoreCase))
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    if (items.Count >= maxRelated)
                        break;
                    var ns = ExtractNamespaces(f);
                    if (!anchorNs.Overlaps(ns))
                        continue;
                    var overlap = anchorNs.Intersect(ns, StringComparer.Ordinal).FirstOrDefault();
                    if (overlap is null)
                        continue;
                    AddIfNew(f, "same_namespace", $"Тот же namespace «{overlap}»");
                }
            }
        }

        if (items.Count < maxRelated)
        {
            var dir = Path.GetDirectoryName(anchor);
            if (!string.IsNullOrEmpty(dir))
            {
                foreach (var p in allKnownFiles
                             .Where(f => string.Equals(Path.GetDirectoryName(f), dir, StringComparison.OrdinalIgnoreCase))
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    if (items.Count >= maxRelated)
                        break;
                    AddIfNew(p, "same_directory", "Тот же каталог");
                }
            }
        }

        return items;
    }

    private static string BuildSubgraph(
        PreparedNavigationInputs ctx,
        int maxNodes,
        int maxEdges,
        int? line,
        int? column)
    {
        var relatedJson = BuildRelated(ctx, Math.Max(maxNodes * 2, DefaultMaxRelated), line, column);
        using var doc = JsonDocument.Parse(relatedJson);
        if (doc.RootElement.TryGetProperty("error", out _))
            return relatedJson;

        var kindFilterPayload = new
        {
            preset = ctx.PresetRequested,
            include_kinds_effective = ctx.KindFilter.EffectiveIncludeKinds,
            exclude_kinds_effective = ctx.KindFilter.EffectiveExcludeKinds
        };

        var anchor = ctx.Anchor;
        var solutionPath = ctx.SolutionPath;
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();
        var nodes = new List<object>
        {
            new { id = "n0", path = anchor, kind = "anchor", label = Path.GetFileName(anchor), relative_path = McpSolutionTree.GetRelativePath(solutionPath, anchor) }
        };
        var edges = new List<object>();
        var idByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [Path.GetFullPath(anchor)] = "n0" };
        var n = 1;
        foreach (var el in items)
        {
            if (nodes.Count >= maxNodes)
                break;
            if (!el.TryGetProperty("path", out var pathEl))
                continue;
            var p = pathEl.GetString();
            if (string.IsNullOrEmpty(p))
                continue;
            var full = Path.GetFullPath(p);
            if (idByPath.ContainsKey(full))
                continue;
            var id = $"n{n++}";
            idByPath[full] = id;
            var relatedKind = el.TryGetProperty("kind", out var kindEl) ? kindEl.GetString() : null;
            var semanticKind = string.IsNullOrEmpty(relatedKind) ? "related" : relatedKind!;
            nodes.Add(new
            {
                id,
                path = full,
                kind = semanticKind,
                label = Path.GetFileName(full),
                relative_path = McpSolutionTree.GetRelativePath(solutionPath, full),
                rationale = el.TryGetProperty("rationale", out var r) ? r.GetString() : null
            });
            if (edges.Count < maxEdges)
                edges.Add(new { from_id = "n0", to_id = id, kind = "related_to", related_kind = relatedKind });
        }

        return JsonSerializer.Serialize(new
        {
            mode = "subgraph",
            graph_kind = CodeNavigationMapGraphKindWire.RelatedFiles,
            anchor_path = anchor,
            line,
            column,
            max_nodes = maxNodes,
            max_edges = maxEdges,
            kind_filter = kindFilterPayload,
            nodes,
            edges
        }, s_compactJson);
    }

    private static IEnumerable<(string path, string rationale)> FindXamlCodeBehindPairs(
        string anchor,
        IReadOnlyList<string> allCs,
        IReadOnlyList<string> markupPaths)
    {
        var name = Path.GetFileName(anchor);
        if (name.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = name[..^".axaml.cs".Length];
            var want = baseName + ".axaml";
            foreach (var m in markupPaths)
            {
                if (string.Equals(Path.GetFileName(m), want, StringComparison.OrdinalIgnoreCase))
                    yield return (m, "Разметка Avalonia (.axaml) для этого code-behind");
            }

            yield break;
        }

        if (name.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = name[..^".xaml.cs".Length];
            var want = baseName + ".xaml";
            foreach (var m in markupPaths)
            {
                if (string.Equals(Path.GetFileName(m), want, StringComparison.OrdinalIgnoreCase))
                    yield return (m, "Разметка WPF (.xaml) для этого code-behind");
            }

            yield break;
        }

        if (name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            var stem = Path.GetFileNameWithoutExtension(anchor);
            var want = stem + ".axaml.cs";
            foreach (var c in allCs)
            {
                if (string.Equals(Path.GetFileName(c), want, StringComparison.OrdinalIgnoreCase))
                    yield return (c, "Code-behind (.axaml.cs) для этой разметки");
            }
        }
        else if (name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            var stem = Path.GetFileNameWithoutExtension(anchor);
            var want = stem + ".xaml.cs";
            foreach (var c in allCs)
            {
                if (string.Equals(Path.GetFileName(c), want, StringComparison.OrdinalIgnoreCase))
                    yield return (c, "Code-behind (.xaml.cs) для этой разметки");
            }
        }
    }

    private static IEnumerable<(string path, string rationale)> FindTestCounterparts(string anchor, IReadOnlyList<string> allCs)
    {
        var stem = Path.GetFileNameWithoutExtension(anchor);
        if (stem.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) && stem.Length > "Tests".Length)
        {
            var baseName = stem[..^"Tests".Length];
            foreach (var f in allCs)
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(f), baseName, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(Path.GetFullPath(f), Path.GetFullPath(anchor), StringComparison.OrdinalIgnoreCase))
                    yield return (f, "Исходный файл для тестового типа (*Tests)");
            }

            yield break;
        }

        if (stem.EndsWith("Test", StringComparison.OrdinalIgnoreCase)
            && !stem.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
            && stem.Length > "Test".Length)
        {
            var baseName = stem[..^"Test".Length];
            foreach (var f in allCs)
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(f), baseName, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(Path.GetFullPath(f), Path.GetFullPath(anchor), StringComparison.OrdinalIgnoreCase))
                    yield return (f, "Исходный файл для тестового типа (*Test)");
            }

            yield break;
        }

        foreach (var suffix in new[] { "Tests", "Test" })
        {
            var wantFile = stem + suffix + ".cs";
            foreach (var f in allCs)
            {
                if (!string.Equals(Path.GetFileName(f), wantFile, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(Path.GetFullPath(f), Path.GetFullPath(anchor), StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return (f, suffix == "Tests" ? "Тесты (*Tests.cs) для этого типа" : "Тесты (*Test.cs) для этого типа");
            }
        }
    }

    private static HashSet<string> ExtractNamespaces(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            if (text.Length > 12000)
                text = text[..12000];
            var tree = CSharpSyntaxTree.ParseText(text, path: path);
            var root = tree.GetRoot();
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ns in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
                set.Add(ns.Name.ToString());
            return set;
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> EnumeratePartialTypeNames(string anchorPath)
    {
        string text;
        try
        {
            text = File.ReadAllText(anchorPath);
        }
        catch
        {
            return [];
        }

        SyntaxTree tree;
        try
        {
            tree = CSharpSyntaxTree.ParseText(text, path: anchorPath);
        }
        catch
        {
            return [];
        }

        var root = tree.GetRoot();
        return root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(t => t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            .Select(t => t.Identifier.Text)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<string> FindPartialPeers(IReadOnlyList<string> allCs, string anchor, string typeName)
    {
        var rx = new Regex($@"\bpartial\s+(?:class|struct|record)\s+{Regex.Escape(typeName)}\b", RegexOptions.CultureInvariant);
        var list = new List<string>();
        foreach (var f in allCs)
        {
            if (string.Equals(Path.GetFullPath(f), Path.GetFullPath(anchor), StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                var head = ReadHead(File.ReadAllText(f));
                if (rx.IsMatch(head))
                    list.Add(f);
            }
            catch
            {
                // skip
            }
        }
        return list;
    }

    private static string ReadHead(string text)
    {
        if (text.Length <= 12000)
            return text;
        return text[..12000];
    }
}

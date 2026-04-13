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
public static class WorkspaceNavigationContextBuilder
{
    public const int DefaultMaxRelated = 32;
    public const int DefaultMaxNodes = 12;
    public const int DefaultMaxEdges = 24;

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
        int maxEdges)
    {
        var anchor = !string.IsNullOrWhiteSpace(anchorPath)
            ? anchorPath.Trim()
            : fallbackCurrentPath?.Trim();
        if (string.IsNullOrEmpty(anchor))
        {
            return JsonSerializer.Serialize(new { error = "no_file", message = "Укажите file_path или откройте файл в редакторе." });
        }

        try
        {
            anchor = Path.GetFullPath(anchor);
        }
        catch
        {
            return JsonSerializer.Serialize(new { error = "bad_path", message = anchor });
        }

        var files = McpSolutionTree.CollectFileEntries(roots)
            .Select(e => e.FullPath)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            return JsonSerializer.Serialize(new { error = "no_solution_files", message = "Нет .cs в дереве решения." });
        }

        var known = new HashSet<string>(files.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        if (!known.Contains(anchor))
        {
            return JsonSerializer.Serialize(new { error = "file_not_in_solution", message = "Файл не из загруженного решения.", path = anchor });
        }

        var m = mode.Trim().ToLowerInvariant();
        return m switch
        {
            "related" => BuildRelated(anchor, files, solutionPath, maxRelated, line, column),
            "subgraph" => BuildSubgraph(anchor, files, solutionPath, maxNodes, maxEdges, line, column),
            _ => JsonSerializer.Serialize(new { error = "bad_mode", message = "mode: related | subgraph", mode })
        };
    }

    private static string BuildRelated(
        string anchor,
        IReadOnlyList<string> allCs,
        string? solutionPath,
        int maxRelated,
        int? line,
        int? column)
    {
        var items = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(anchor) };

        foreach (var name in EnumeratePartialTypeNames(anchor))
        {
            foreach (var peer in FindPartialPeers(allCs, anchor, name))
            {
                if (items.Count >= maxRelated)
                    goto FillSameDir;
                var full = Path.GetFullPath(peer);
                if (seen.Contains(full))
                    continue;
                seen.Add(full);
                items.Add(new
                {
                    path = full,
                    kind = "partial_peer",
                    rationale = $"Partial того же типа «{name}»",
                    relative_path = McpSolutionTree.GetRelativePath(solutionPath, full)
                });
            }
        }

        FillSameDir:
        var dir = Path.GetDirectoryName(anchor);
        if (!string.IsNullOrEmpty(dir) && items.Count < maxRelated)
        {
            foreach (var p in allCs
                         .Where(f => string.Equals(Path.GetDirectoryName(f), dir, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                if (items.Count >= maxRelated)
                    break;
                var full = Path.GetFullPath(p);
                if (seen.Contains(full))
                    continue;
                seen.Add(full);
                items.Add(new
                {
                    path = full,
                    kind = "same_directory",
                    rationale = "Тот же каталог",
                    relative_path = McpSolutionTree.GetRelativePath(solutionPath, full)
                });
            }
        }

        var payload = new
        {
            mode = "related",
            anchor_path = anchor,
            line,
            column,
            max_related = maxRelated,
            items
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string BuildSubgraph(
        string anchor,
        IReadOnlyList<string> allCs,
        string? solutionPath,
        int maxNodes,
        int maxEdges,
        int? line,
        int? column)
    {
        var relatedJson = BuildRelated(anchor, allCs, solutionPath, Math.Max(maxNodes * 2, DefaultMaxRelated), line, column);
        using var doc = JsonDocument.Parse(relatedJson);
        if (doc.RootElement.TryGetProperty("error", out _))
            return relatedJson;

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
            nodes.Add(new
            {
                id,
                path = full,
                kind = "related",
                label = Path.GetFileName(full),
                relative_path = McpSolutionTree.GetRelativePath(solutionPath, full),
                rationale = el.TryGetProperty("rationale", out var r) ? r.GetString() : null
            });
            if (edges.Count < maxEdges)
                edges.Add(new { from_id = "n0", to_id = id, kind = "related_to" });
        }

        return JsonSerializer.Serialize(new
        {
            mode = "subgraph",
            anchor_path = anchor,
            line,
            column,
            max_nodes = maxNodes,
            max_edges = maxEdges,
            nodes,
            edges
        });
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

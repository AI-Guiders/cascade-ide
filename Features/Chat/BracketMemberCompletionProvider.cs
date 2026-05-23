#nullable enable

using System.Collections.Concurrent;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Features.Chat;

/// <summary>Члены типов в .cs для оси <c>M:</c> bracket-autocomplete.</summary>
public static class BracketMemberCompletionProvider
{
    public sealed record Match(string Name, string Help);

    private sealed record FileIndexCache(long MtimeUtcTicks, IReadOnlyList<Match> Members);

    private static readonly ConcurrentDictionary<string, FileIndexCache> IndexByAbsolutePath =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Проактивно построить индекс членов для файла (ADR 0141 P0).</summary>
    public static void WarmIndex(string? filePath, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (!TryResolveAbsolute(filePath, workspaceRoot, out var absolute))
            return;

        if (!absolute.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;

        long mtimeTicks;
        try
        {
            mtimeTicks = File.GetLastWriteTimeUtc(absolute).Ticks;
        }
        catch
        {
            return;
        }

        _ = loadOrBuildIndex(absolute, mtimeTicks);
    }

    public static IReadOnlyList<Match> GetMatches(
        string? filePath,
        string? workspaceRoot,
        string namePrefix,
        int limit)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(filePath))
            return [];

        if (!TryResolveAbsolute(filePath, workspaceRoot, out var absolute))
            return [];

        if (!absolute.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return [];

        long mtimeTicks;
        try
        {
            mtimeTicks = File.GetLastWriteTimeUtc(absolute).Ticks;
        }
        catch
        {
            return [];
        }

        var all = loadOrBuildIndex(absolute, mtimeTicks);
        var prefix = namePrefix.Trim();

        IEnumerable<Match> ranked = all;
        if (prefix.Length > 0)
            ranked = ranked.Where(m => m.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return ranked
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static IReadOnlyList<Match> loadOrBuildIndex(string absolute, long mtimeTicks)
    {
        if (IndexByAbsolutePath.TryGetValue(absolute, out var cached) && cached.MtimeUtcTicks == mtimeTicks)
            return cached.Members;

        var built = buildIndex(absolute);
        IndexByAbsolutePath[absolute] = new FileIndexCache(mtimeTicks, built);
        return built;
    }

    private static IReadOnlyList<Match> buildIndex(string absolute)
    {
        string text;
        try
        {
            text = File.ReadAllText(absolute);
        }
        catch
        {
            return [];
        }

        var tree = CSharpSyntaxTree.ParseText(SourceText.From(text, Encoding.UTF8), path: absolute);
        var root = tree.GetCompilationUnitRoot();
        if (root is null)
            return [];

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case MethodDeclarationSyntax m:
                    add(names, m.Identifier.Text, "method");
                    break;
                case PropertyDeclarationSyntax p:
                    add(names, p.Identifier.Text, "property");
                    break;
                case ConstructorDeclarationSyntax c:
                    add(names, c.Identifier.Text, "ctor");
                    break;
                case IndexerDeclarationSyntax:
                    add(names, "this", "indexer");
                    break;
                case EventDeclarationSyntax ev:
                    add(names, ev.Identifier.Text, "event");
                    break;
                case FieldDeclarationSyntax f:
                    foreach (var v in f.Declaration.Variables)
                        add(names, v.Identifier.Text, "field");
                    break;
                case ClassDeclarationSyntax:
                case StructDeclarationSyntax:
                case InterfaceDeclarationSyntax:
                case RecordDeclarationSyntax:
                    var typeName = node switch
                    {
                        ClassDeclarationSyntax c => c.Identifier.Text,
                        StructDeclarationSyntax s => s.Identifier.Text,
                        InterfaceDeclarationSyntax i => i.Identifier.Text,
                        RecordDeclarationSyntax rec => rec.Identifier.Text,
                        _ => "",
                    };
                    if (!string.IsNullOrWhiteSpace(typeName))
                        add(names, typeName, "type");
                    break;
            }
        }

        return names
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => new Match(p.Key, p.Value))
            .ToList();
    }

    private static void add(Dictionary<string, string> names, string name, string kind)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        names.TryAdd(name, kind);
    }

    private static bool TryResolveAbsolute(string filePath, string? workspaceRoot, out string absolute)
    {
        absolute = filePath.Trim();
        if (Path.IsPathRooted(absolute))
            return File.Exists(absolute);

        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return false;

        var combined = Path.Combine(workspaceRoot.Trim(), absolute.Replace('/', Path.DirectorySeparatorChar));
        absolute = Path.GetFullPath(combined);
        return File.Exists(absolute);
    }
}

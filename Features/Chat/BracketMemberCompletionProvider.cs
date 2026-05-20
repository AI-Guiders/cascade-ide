#nullable enable

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

        string text;
        try
        {
            text = File.ReadAllText(absolute);
        }
        catch
        {
            return [];
        }

        var prefix = namePrefix.Trim();
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
                case ClassDeclarationSyntax cl:
                case StructDeclarationSyntax st:
                case InterfaceDeclarationSyntax it:
                case RecordDeclarationSyntax r:
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

        IEnumerable<KeyValuePair<string, string>> ranked = names;
        if (prefix.Length > 0)
            ranked = ranked.Where(p => p.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return ranked
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
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

#nullable enable

using CascadeIDE.Models.Editor;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services.Intercom;

/// <summary>Сбор symbol sidecar из синтаксического дерева (ADR 0135).</summary>
public static class IntercomSymbolLineIndexBuilder
{
    public static IReadOnlyList<IntercomSymbolLineEntry> CollectFromFile(string absolutePath)
    {
        if (!absolutePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(absolutePath))
            return [];

        string text;
        try
        {
            text = File.ReadAllText(absolutePath);
        }
        catch
        {
            return [];
        }

        return CollectFromText(text, absolutePath);
    }

    /// <summary>Собрать symbol entries из уже прочитанного текста (HCI reindex observer, ADR 0135).</summary>
    public static IReadOnlyList<IntercomSymbolLineEntry> CollectFromText(string text, string absolutePath)
    {
        if (!absolutePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(text))
        {
            return [];
        }

        var tree = CSharpSyntaxTree.ParseText(text, path: absolutePath);
        if (tree.GetCompilationUnitRoot() is not { } root)
            return [];

        var compilation = buildCompilation(absolutePath, tree);
        var model = compilation.GetSemanticModel(tree);
        var entries = new List<IntercomSymbolLineEntry>();
        var simpleCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var candidate in root.DescendantNodes())
        {
            if (!tryGetDeclaredSymbol(candidate, model, out var sym) || sym is null)
                continue;

            if (!tryLineRangeFromNode(tree, candidate, out var start, out var end))
                continue;

            var docId = sym.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                entries.Add(new IntercomSymbolLineEntry("docid", docId, start, end));
            }

            var simple = sym.Name;
            if (!string.IsNullOrWhiteSpace(simple))
                simpleCounts[simple] = simpleCounts.GetValueOrDefault(simple) + 1;
        }

        foreach (var candidate in root.DescendantNodes())
        {
            if (!tryGetDeclaredSymbol(candidate, model, out var sym) || sym is null)
                continue;

            if (!tryLineRangeFromNode(tree, candidate, out var start, out var end))
                continue;

            var simple = sym.Name;
            if (string.IsNullOrWhiteSpace(simple) || simpleCounts.GetValueOrDefault(simple) != 1)
                continue;

            entries.Add(new IntercomSymbolLineEntry("simple", simple, start, end));
        }

        return entries;
    }

    public static void IndexFile(in IntercomAttachResolveCacheContext cache, string absolutePath, string relativePath)
    {
        IndexFile(cache, absolutePath, relativePath, text: null, lastWriteUtcTicks: null);
    }

    public static void IndexFile(
        in IntercomAttachResolveCacheContext cache,
        string absolutePath,
        string relativePath,
        string? text,
        long? lastWriteUtcTicks)
    {
        if (!File.Exists(absolutePath))
            return;

        long mtime;
        try
        {
            mtime = lastWriteUtcTicks ?? File.GetLastWriteTimeUtc(absolutePath).Ticks;
        }
        catch
        {
            return;
        }

        var entries = text is null
            ? CollectFromFile(absolutePath)
            : CollectFromText(text, absolutePath);
        IntercomSymbolLineIndex.ReplaceFileSymbols(cache, relativePath, mtime, entries);
    }

    private static bool tryGetDeclaredSymbol(SyntaxNode candidate, SemanticModel model, out ISymbol? symbol)
    {
        symbol = candidate switch
        {
            MethodDeclarationSyntax m => model.GetDeclaredSymbol(m),
            PropertyDeclarationSyntax p => model.GetDeclaredSymbol(p),
            FieldDeclarationSyntax f => model.GetDeclaredSymbol(f.Declaration.Variables.First()),
            VariableDeclaratorSyntax v => model.GetDeclaredSymbol(v),
            ConstructorDeclarationSyntax c => model.GetDeclaredSymbol(c),
            IndexerDeclarationSyntax i => model.GetDeclaredSymbol(i),
            EventDeclarationSyntax e => model.GetDeclaredSymbol(e),
            ClassDeclarationSyntax cl => model.GetDeclaredSymbol(cl),
            StructDeclarationSyntax st => model.GetDeclaredSymbol(st),
            InterfaceDeclarationSyntax it => model.GetDeclaredSymbol(it),
            RecordDeclarationSyntax r => model.GetDeclaredSymbol(r),
            _ => null,
        };
        return symbol is not null;
    }

    private static bool tryLineRangeFromNode(SyntaxTree tree, SyntaxNode node, out int start, out int end)
    {
        start = 0;
        end = 0;
        var span = node.Span;
        if (span.IsEmpty)
            return false;

        var startLine = RoslynLinePositionMapper.ToEditorLineNumber(tree.GetLineSpan(span).StartLinePosition);
        var endSpan = span.Length > 0 ? new Microsoft.CodeAnalysis.Text.TextSpan(Math.Max(span.Start, span.End - 1), 1) : span;
        var endLine = RoslynLinePositionMapper.ToEditorLineNumber(tree.GetLineSpan(endSpan).StartLinePosition);

        start = startLine.Value;
        end = endLine.Value;
        return end >= start;
    }

    private static CSharpCompilation buildCompilation(string filePath, SyntaxTree tree)
    {
        var refs = new List<MetadataReference>();
        void addRef(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            refs.Add(MetadataReference.CreateFromFile(path));
        }

        addRef(typeof(object).Assembly.Location);
        addRef(typeof(Enumerable).Assembly.Location);

        return CSharpCompilation.Create(
            "CascadeSymbolIndex_" + Guid.NewGuid().ToString("N"),
            new[] { tree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

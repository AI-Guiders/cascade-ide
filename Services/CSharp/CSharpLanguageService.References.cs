using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    public sealed record ReferenceLocation(string FilePath, int Line, int Column);

    /// <summary>Find references in the current file (Roslyn fast path).</summary>
    public IReadOnlyList<ReferenceLocation> FindReferencesInFile(
        string filePath,
        string sourceText,
        int line,
        int column,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1)
            return [];

        try
        {
            var text = SourceText.From(sourceText);
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count)
                return [];

            var lineInfo = lines[line - 1];
            var position = lineInfo.Start + Math.Min(Math.Max(0, column - 1), lineInfo.Span.Length);
            var root = model.SyntaxTree.GetRoot(ct);
            var symbol = TryResolveSymbolAtPosition(model, root, position, ct);
            if (symbol is null)
                return [];

            var seen = new HashSet<(int line, int col)>();
            var list = new List<ReferenceLocation>();
        foreach (var node in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var info = model.GetSymbolInfo(node, ct);
            if (!SymbolEqualityComparer.Default.Equals(info.Symbol, symbol))
                continue;

                var span = node.Span;
                var pos = text.Lines.GetLinePosition(span.Start);
                var key = (pos.Line, pos.Character);
                if (!seen.Add(key))
                    continue;

                list.Add(new ReferenceLocation(filePath, pos.Line + 1, pos.Character + 1));
            }

            list.Sort(static (a, b) =>
            {
                var lineCmp = a.Line.CompareTo(b.Line);
                return lineCmp != 0 ? lineCmp : a.Column.CompareTo(b.Column);
            });
            return list;
        }
        catch
        {
            return [];
        }
    }

    private static ISymbol? TryResolveSymbolAtPosition(
        SemanticModel model,
        SyntaxNode root,
        int position,
        CancellationToken ct)
    {
        var token = root.FindToken(position, findInsideTrivia: true);
        for (var node = token.Parent; node is not null; node = node.Parent)
        {
            var info = model.GetSymbolInfo(node, ct);
            var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            if (symbol is not null)
                return symbol;
        }

        return null;
    }
}

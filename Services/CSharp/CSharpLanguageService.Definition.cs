using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    public sealed record DefinitionLocation(string FilePath, int Line, int Column);

    /// <summary>Go-to-definition target for Roslyn fast path (1-based line/column).</summary>
    public DefinitionLocation? TryGetDefinitionLocation(
        string filePath,
        string sourceText,
        int line,
        int column,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1)
            return null;

        try
        {
            var text = SourceText.From(sourceText);
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count)
                return null;

            var lineInfo = lines[line - 1];
            var position = lineInfo.Start + Math.Min(Math.Max(0, column - 1), lineInfo.Span.Length);
            var root = model.SyntaxTree.GetRoot(ct);
            var token = root.FindToken(position, findInsideTrivia: true);
            ISymbol? symbol = null;
            for (var node = token.Parent; node is not null; node = node.Parent)
            {
                var info = model.GetSymbolInfo(node, ct);
                symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
                if (symbol is not null)
                    break;
            }

            if (symbol?.Locations.FirstOrDefault() is not { } location)
                return null;
            if (location.SourceTree is null)
                return null;

            var defPath = location.SourceTree.FilePath;
            if (string.IsNullOrWhiteSpace(defPath))
                defPath = filePath;

            var linePos = location.GetLineSpan().StartLinePosition;
            return new DefinitionLocation(
                defPath,
                linePos.Line + 1,
                linePos.Character + 1);
        }
        catch
        {
            return null;
        }
    }
}

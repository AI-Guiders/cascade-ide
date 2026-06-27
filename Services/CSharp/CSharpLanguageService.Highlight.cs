using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    /// <summary>Диапазоны подсветки вхождений символа в том же файле (offset, length). Выполнять в фоне.</summary>
    public IReadOnlyList<TextSpan> GetHighlightSpans(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1)
            return [];

        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_highlightCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count)
                return [];

            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var token = root.FindToken(position, findInsideTrivia: true);
            ISymbol? symbol = null;
            for (var node = token.Parent; node is not null; node = node.Parent)
            {
                symbol = model.GetDeclaredSymbol(node, ct) ?? model.GetSymbolInfo(node, ct).Symbol;
                if (symbol is not null)
                    break;
            }

            if (symbol is null)
            {
                _highlightCache[cacheKey] = [];
                return [];
            }

            var spans = new List<TextSpan>();
            foreach (var node in root.DescendantNodes())
            {
                ct.ThrowIfCancellationRequested();
                if (!TryGetOccurrenceSpan(node, out var occurrenceSpan))
                    continue;
                if (model.GetSymbolInfo(node, ct).Symbol?.Equals(symbol, SymbolEqualityComparer.Default) != true)
                    continue;
                spans.Add(occurrenceSpan);
            }

            TrimCaches(_highlightCache);
            _highlightCache[cacheKey] = spans;
            return spans;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Leaf occurrence span only — not whole <c>new T { … }</c> / invocation trees (VS-style).</summary>
    private static bool TryGetOccurrenceSpan(SyntaxNode node, out TextSpan span)
    {
        switch (node)
        {
            case IdentifierNameSyntax id:
                span = id.Identifier.Span;
                return true;
            case GenericNameSyntax gen:
                span = gen.Identifier.Span;
                return true;
            case MemberAccessExpressionSyntax memberAccess:
                span = memberAccess.Name.Span;
                return true;
            default:
                span = default;
                return false;
        }
    }
}

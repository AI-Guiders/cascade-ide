using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    /// <summary>Подсказка по параметрам (сигнатура метода) в позиции. Выполнять в фоне.</summary>
    public string? GetSignatureHelp(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1) return null;
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_signatureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count) return null;
            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var node = root.FindNode(new TextSpan(position, 0), findInsideTrivia: true);
            InvocationExpressionSyntax? invocation = null;
            for (var n = node; n is not null; n = n.Parent)
            {
                if (n is InvocationExpressionSyntax inv)
                {
                    invocation = inv;
                    break;
                }
            }
            if (invocation is null) return null;

            var symbolInfo = model.GetSymbolInfo(invocation, ct);
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                var sig = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                TrimCaches(_signatureCache);
                _signatureCache[cacheKey] = sig;
                return sig;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}

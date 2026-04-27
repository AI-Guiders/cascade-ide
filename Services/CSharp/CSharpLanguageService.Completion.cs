using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    /// <summary>Возвращает предложения автодополнения в позиции (1-based line, column). Выполнять в фоне.</summary>
    public IReadOnlyList<CompletionItem> GetCompletionItems(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1) return [];
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_completionCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count) return [];
            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var token = root.FindToken(position, findInsideTrivia: true);
            var list = new List<CompletionItem>();

            // После точки — члены типа
            if (position > 0 && position <= text.Length && sourceText[position - 1] == '.')
            {
                var expr = token.Parent?.Parent as ExpressionSyntax;
                if (expr is not null)
                {
                    var symbolInfo = model.GetSymbolInfo(expr, ct);
                    if (symbolInfo.Symbol is ITypeSymbol typeSymbol)
                    {
                        foreach (var member in typeSymbol.GetMembers().Where(m => m.CanBeReferencedByName && !m.IsImplicitlyDeclared))
                        {
                            var insert = member.Name;
                            if (member is IMethodSymbol method && method.Parameters.Length > 0)
                                insert += "(";
                            list.Add(new CompletionItem(member.Name, insert, member.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                        }
                    }
                }
            }
            else
            {
                // Ключевые слова C#
                var keywords = new[] { "abstract", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "get", "goto", "if", "implicit", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var", "virtual", "void", "volatile", "while" };
                list.AddRange(keywords.Select(k => new CompletionItem(k, k)));

                // Символы в области видимости
                var semanticModel = model;
                var lookupPosition = token.SpanStart;
                foreach (var symbol in semanticModel.LookupSymbols(lookupPosition))
                {
                    if (symbol.CanBeReferencedByName && !symbol.IsImplicitlyDeclared && !list.Any(c => c.InsertText == symbol.Name))
                    {
                        var insert = symbol.Name;
                        if (symbol is IMethodSymbol ms && ms.Parameters.Length > 0)
                            insert += "(";
                        list.Add(new CompletionItem(symbol.Name, insert, symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    }
                }
            }

            TrimCaches(_completionCache);
            _completionCache[cacheKey] = list;
            return list;
        }
        catch
        {
            return [];
        }
    }
}

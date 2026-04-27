using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    private static readonly SymbolDisplayFormat QuickInfoDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters
                       | SymbolDisplayMemberOptions.IncludeType
                       | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                              | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private static string? TryExtractSummaryFromDocumentationXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;
        var m = Regex.Match(xml, @"<summary>\s*(.*?)\s*</summary>", RegexOptions.Singleline);
        if (!m.Success)
            return null;
        var inner = Regex.Replace(m.Groups[1].Value, @"<.*?>", "");
        inner = Regex.Replace(inner, @"\s+", " ").Trim();
        return string.IsNullOrEmpty(inner) ? null : inner;
    }

    /// <summary>Краткая подсказка по символу в позиции (1-based line, column), как Quick Info в IDE. Выполнять в фоне.</summary>
    public string? GetQuickInfo(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1)
            return null;
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_quickInfoCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count)
                return null;
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

            if (symbol is null && token.Parent is not null)
            {
                var info = model.GetSymbolInfo(token.Parent, ct);
                symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            }

            if (symbol is null)
            {
                TrimCaches(_quickInfoCache);
                _quickInfoCache[cacheKey] = null;
                return null;
            }

            var display = symbol.ToDisplayString(QuickInfoDisplayFormat);
            var summary = TryExtractSummaryFromDocumentationXml(symbol.GetDocumentationCommentXml());
            var result = summary is not null ? $"{display}\n\n{summary}" : display;
            TrimCaches(_quickInfoCache);
            _quickInfoCache[cacheKey] = result;
            return result;
        }
        catch
        {
            return null;
        }
    }
}

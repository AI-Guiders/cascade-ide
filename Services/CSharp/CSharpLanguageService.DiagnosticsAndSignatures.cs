using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    private static readonly SymbolDisplayFormat SignatureFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Диагностики по файлу для Problems / MCP: только <b>лексика и синтаксис</b> (парсер Roslyn).
    /// Полная <c>CSharpCompilation</c> у нас без ссылок на пакеты и соседние файлы проекта — семантические
    /// ошибки (CS0246 и т.д.) были бы ложными при успешной сборке MSBuild. Семантика — из вывода сборки.
    /// </summary>
    public IReadOnlyList<Diagnostic> GetDiagnosticsForFile(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return [];
        try
        {
            var text = SourceText.From(sourceText);
            var tree = CSharpSyntaxTree.ParseText(text, path: filePath, cancellationToken: ct);
            var list = new List<Diagnostic>();
            foreach (var d in tree.GetDiagnostics(ct))
            {
                if (d.Location.SourceTree != tree)
                    continue;
                if (d.Severity == DiagnosticSeverity.Error)
                    list.Add(d);
                else if (d.Severity == DiagnosticSeverity.Warning && list.Count < 50)
                    list.Add(d);
            }

            return list;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Сигнатуры публичных типов и членов в файле (одна строка на объявление) для минимизации контекста.</summary>
    public IReadOnlyList<string> GetSignatureStringsForFile(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return [];
        try
        {
            var text = SourceText.From(sourceText);
            var model = GetOrCreateModel(filePath, text, ct);
            var root = model.SyntaxTree.GetRoot(ct);
            var list = new List<string>();
            foreach (var node in root.DescendantNodes())
            {
                ct.ThrowIfCancellationRequested();
                var symbol = model.GetDeclaredSymbol(node, ct);
                if (symbol is null) continue;
                if (symbol.DeclaredAccessibility != Accessibility.Public && symbol.ContainingType?.DeclaredAccessibility != Accessibility.Public)
                    continue;
                var line = symbol.ToDisplayString(SignatureFormat);
                if (string.IsNullOrEmpty(line)) continue;
                list.Add(line);
                if (list.Count >= 200) break;
            }
            return list;
        }
        catch
        {
            return [];
        }
    }
}

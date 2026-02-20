using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services;

/// <summary>Минимизация контекста для чата: только диагностики и сигнатуры (встроенный Roslyn).</summary>
public sealed class ContextMinimizer
{
    private readonly CSharpLanguageService _languageService;

    public ContextMinimizer(CSharpLanguageService languageService)
    {
        _languageService = languageService;
    }

    /// <summary>Возвращает компактный текст: блок Diagnostics (file:line:severity: id: message) и блок Signatures.</summary>
    /// <param name="filePath">Путь к файлу (для .cs используется анализ).</param>
    /// <param name="sourceText">Исходный код файла.</param>
    /// <param name="ct">Отмена.</param>
    /// <returns>Строка для подмешивания в системное/первое сообщение; пустая, если не C# или при ошибке.</returns>
    public string Minimize(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var diagnostics = _languageService.GetDiagnosticsForFile(filePath, sourceText, ct);
        var signatures = _languageService.GetSignatureStringsForFile(filePath, sourceText, ct);
        if (diagnostics.Count == 0 && signatures.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        if (diagnostics.Count > 0)
        {
            sb.Append("## Diagnostics\n");
            var fileName = System.IO.Path.GetFileName(filePath);
            foreach (var d in diagnostics)
            {
                var line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
                var col = d.Location.GetLineSpan().StartLinePosition.Character + 1;
                sb.Append(fileName).Append('(').Append(line).Append(',').Append(col).Append("): ")
                    .Append(d.Severity == DiagnosticSeverity.Error ? "error" : "warning")
                    .Append(' ').Append(d.Id).Append(": ").Append(d.GetMessage()).Append('\n');
            }
        }
        if (signatures.Count > 0)
        {
            sb.Append("## Signatures\n");
            foreach (var sig in signatures)
                sb.Append(sig).Append('\n');
        }
        return sb.ToString();
    }
}

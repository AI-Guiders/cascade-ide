using System.Text.Json;
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
                var (line, col) = RoslynLinePositionMapper.ToEditorLineColumn(d.Location.GetLineSpan().StartLinePosition);
                sb.Append(fileName).Append('(').Append(line.Value).Append(',').Append(col.Value).Append("): ")
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

    /// <summary>Диагностики по файлу в виде JSON для MCP (ide_get_current_file_diagnostics): массив { id, message, severity, line, column } (line/column 1-based). Только .cs.</summary>
    public string GetDiagnosticsJson(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return "[]";
        var diagnostics = _languageService.GetDiagnosticsForFile(filePath, sourceText, ct);
        var list = diagnostics.Select(d =>
        {
            var (line, col) = RoslynLinePositionMapper.ToEditorLineColumn(d.Location.GetLineSpan().StartLinePosition);
            return new { id = d.Id, message = d.GetMessage(), severity = d.Severity == DiagnosticSeverity.Error ? "error" : "warning", line = line.Value, column = col.Value };
        }).ToList();
        return JsonSerializer.Serialize(list);
    }
}

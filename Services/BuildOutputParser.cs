using System.Text.RegularExpressions;

namespace CascadeIDE.Services;

/// <summary>Один элемент вывода сборки: ошибка или предупреждение (файл, строка, столбец, код, сообщение).</summary>
public sealed record BuildDiagnostic(string File, int Line, int? Column, string? Code, string Message);

/// <summary>Результат разбора вывода dotnet build (MSBuild-формат: path(line,col): error/warning code: message).</summary>
public sealed record BuildParseResult(
    int ExitCode,
    IReadOnlyList<BuildDiagnostic> Errors,
    IReadOnlyList<BuildDiagnostic> Warnings)
{
    public bool Success => ExitCode == 0 && Errors.Count == 0;
}

public static class BuildOutputParser
{
    // path(line,col): error CS1234: message  или  path(line): error CS1234: message
    // path может содержать двоеточие (C:\...), поэтому берём минимальное совпадение до (digit
    private static readonly Regex DiagnosticRegex = new(
        @"^(.+?)\((\d+)(?:,(\d+))?\):\s*(error|warning)\s*(\S*?):\s*(.*)$",
        RegexOptions.Compiled);

    private static readonly Regex ExitCodeRegex = new(
        @"Exit code:\s*(\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Разбирает вывод dotnet build: извлекает ошибки, предупреждения и при наличии — код выхода.</summary>
    public static BuildParseResult Parse(string output)
    {
        if (string.IsNullOrEmpty(output))
            return new BuildParseResult(0, [], []);

        var errors = new List<BuildDiagnostic>();
        var warnings = new List<BuildDiagnostic>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var s = line.Trim();
            if (s.Length == 0) continue;

            var m = DiagnosticRegex.Match(s);
            if (!m.Success) continue;

            var file = m.Groups[1].Value.Trim().Trim('"');
            var lineNum = int.Parse(m.Groups[2].ValueSpan);
            var col = m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? int.Parse(m.Groups[3].ValueSpan) : (int?)null;
            var severity = m.Groups[4].Value;
            var code = m.Groups[5].Success ? m.Groups[5].Value : null;
            if (string.IsNullOrWhiteSpace(code)) code = null;
            var message = m.Groups[6].Value.Trim();

            var d = new BuildDiagnostic(file, lineNum, col, code, message);
            if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
                errors.Add(d);
            else
                warnings.Add(d);
        }

        var exitCode = 0;
        var exitMatch = ExitCodeRegex.Match(output);
        if (exitMatch.Success)
            exitCode = int.Parse(exitMatch.Groups[1].ValueSpan);

        return new BuildParseResult(exitCode, errors, warnings);
    }
}

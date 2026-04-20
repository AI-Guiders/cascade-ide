namespace CascadeIDE.Services;

/// <summary>
/// Подсчёт непустых строк (после trim пустые отбрасываются) — тот же смысл, что <c>loc</c> в <c>get_code_metrics</c>.
/// </summary>
public static class SourceLineMetrics
{
    /// <summary>Число строк, где есть хотя бы один непробельный символ; разделитель — <c>\n</c> (как в <see cref="McpCodeMetrics"/>).</summary>
    public static int CountNonEmptyLines(string text) =>
        text.Split('\n').Count(static l => !string.IsNullOrWhiteSpace(l));
}

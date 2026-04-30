namespace CascadeIDE.Services;

/// <summary>
/// Опциональная отладка inlay: каталог <see cref="InlayLogPaths" /> (по умолчанию <c>…/cascade-ide/.inlay-logs</c> рядом с <c>CascadeIDE.sln</c>, вне <c>bin</c>).
/// <list type="bullet">
/// <item><c>CASCADE_INLAY_TRACE=1</c> (или <c>true</c>) — семантика: <c>inlay-trace.log</c>.</item>
/// <item><c>CASCADE_INLAY_DEBUG=1</c> — <c>inlay-debug.log</c>, <c>VarInlay</c> / <c>VisualLine</c> (тот же каталог).</item>
/// <item>Явный путь: <c>CASCADE_INLAY_LOG_DIR</c> (абсолютный каталог) перекрывает разрешение по умолчанию.</item>
/// </list>
/// </summary>
internal static class InlayHintTrace
{
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("CASCADE_INLAY_TRACE"), "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("CASCADE_INLAY_TRACE"), "true", StringComparison.OrdinalIgnoreCase);

    public static bool IsDebug =>
        string.Equals(Environment.GetEnvironmentVariable("CASCADE_INLAY_DEBUG"), "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("CASCADE_INLAY_DEBUG"), "true", StringComparison.OrdinalIgnoreCase);

    public static void Log(string message)
    {
        if (!IsEnabled) return;
        var line = $"{DateTimeOffset.Now:O} {message}";
        // В stdio MCP stdout зарезервирован под JSON-RPC; диагностический текст туда не пишем.

        try
        {
            var dir = InlayLogPaths.GetLogDirectory();
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "inlay-trace.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>Точка отрисовки inlay (<see cref="VarInlayHintElementGenerator" /> / <c>InlayRun.Draw</c>).</summary>
    public static void LogInlayDraw(string message) => LogDebug("InlayDraw " + message);

    public static void LogDebug(string message)
    {
        if (!IsDebug) return;
        var line = $"{DateTimeOffset.Now:O} {message}";
        // В stdio MCP stdout зарезервирован под JSON-RPC; диагностический текст туда не пишем.

        try
        {
            var dir = InlayLogPaths.GetLogDirectory();
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "inlay-debug.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
            // ignore
        }
    }
}

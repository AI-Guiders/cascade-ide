using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Разбор JSON-аргументов MCP для панели отладки (breakpoints / stack / variables).
/// Чистая логика для план B и unit-тестов без IDE command executor.
/// </summary>
public static class McpDebugPayloadParsing
{
    public const string MissingBreakpointsMessage = "Missing breakpoints (array of { file_path, line })";

    /// <summary>
    /// Парсит массив <c>breakpoints</c>: объекты с <c>file_path</c> и <c>line</c>.
    /// </summary>
    public static bool TryParseBreakpoints(
        IReadOnlyDictionary<string, JsonElement>? args,
        out List<(string FilePath, int Line)> breakpoints,
        out string errorMessage)
    {
        breakpoints = [];
        if (args is null || !args.TryGetValue("breakpoints", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            errorMessage = MissingBreakpointsMessage;
            return false;
        }

        foreach (var item in arr.EnumerateArray())
        {
            if (!item.TryGetProperty("file_path", out var fp) || !item.TryGetProperty("line", out var ln))
                continue;
            var path = fp.GetString();
            if (string.IsNullOrEmpty(path))
                continue;
            breakpoints.Add((path, ln.GetInt32()));
        }

        errorMessage = "";
        return true;
    }

    /// <summary>
    /// Опциональные <c>stack_frames</c> и <c>variables</c>; при отсутствии — пустые списки.
    /// </summary>
    public static void ParseDebugState(
        IReadOnlyDictionary<string, JsonElement>? args,
        out List<(string Name, string? File, int Line)> stackFrames,
        out List<(string Name, string Value)> variables)
    {
        stackFrames = [];
        variables = [];
        if (args is null)
            return;

        if (args.TryGetValue("stack_frames", out var sf) && sf.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in sf.EnumerateArray())
            {
                stackFrames.Add((
                    item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    item.TryGetProperty("file", out var f) ? f.GetString() : null,
                    item.TryGetProperty("line", out var l) ? l.GetInt32() : 0));
            }
        }

        if (args.TryGetValue("variables", out var v) && v.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in v.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var vn) && item.TryGetProperty("value", out var vv))
                    variables.Add((vn.GetString() ?? "", vv.GetString() ?? ""));
            }
        }
    }
}

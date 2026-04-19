using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Чтение примитивов из словаря аргументов MCP-команд IDE (JSON).</summary>
public static class McpCommandJsonArgs
{
    public static string? String(IReadOnlyDictionary<string, JsonElement>? args, string key) =>
        args is not null && args.TryGetValue(key, out var e) ? e.GetString() : null;

    public static int Int(IReadOnlyDictionary<string, JsonElement>? args, string key, int defaultValue = 0)
    {
        if (args is null || !args.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Number)
            return defaultValue;
        return e.TryGetInt32(out var v) ? v : defaultValue;
    }

    public static int? OptionalInt32(IReadOnlyDictionary<string, JsonElement>? args, string key) =>
        args is not null && args.TryGetValue(key, out var e) && e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v)
            ? v
            : null;

    public static bool Bool(IReadOnlyDictionary<string, JsonElement>? args, string key, bool defaultValue = false) =>
        args is not null && args.TryGetValue(key, out var e) && (e.ValueKind is JsonValueKind.True or JsonValueKind.False)
            ? e.GetBoolean()
            : defaultValue;

    public static List<string>? StringList(IReadOnlyDictionary<string, JsonElement>? args, string key)
    {
        if (args is null || !args.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Array)
            return null;
        var values = new List<string>();
        foreach (var item in e.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }
        return values;
    }
}

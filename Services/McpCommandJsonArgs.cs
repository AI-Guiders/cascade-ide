using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Чтение примитивов из словаря аргументов MCP-команд IDE (JSON).</summary>
public static class McpCommandJsonArgs
{
    public static string? String(IReadOnlyDictionary<string, JsonElement>? args, string key) =>
        args is not null && args.TryGetValue(key, out var e) ? e.GetString() : null;

    /// <summary>Primary knowledge root override (MCP 2.0 / IDE parity). Accepts legacy <c>canon_path</c> alias.</summary>
    public static string? KnowledgePath(IReadOnlyDictionary<string, JsonElement>? args) =>
        String(args, "knowledge_path") ?? String(args, "canon_path");

    /// <summary>Knowledge root id from TOML <c>[knowledge.roots]</c> / <c>[[knowledge.read_only]]</c> (MCP 2.1 / ADR 015). Mutually exclusive with <see cref="KnowledgePath"/>.</summary>
    public static string? KnowledgeRootId(IReadOnlyDictionary<string, JsonElement>? args) =>
        String(args, "knowledge_root_id");

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

    public static bool? OptionalBool(IReadOnlyDictionary<string, JsonElement>? args, string key)
    {
        if (args is null || !args.TryGetValue(key, out var e))
            return null;
        return e.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    public static double? OptionalDouble(IReadOnlyDictionary<string, JsonElement>? args, string key)
    {
        if (args is null || !args.TryGetValue(key, out var e))
            return null;
        return e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var d)
            ? d
            : null;
    }

    public static long? OptionalInt64(IReadOnlyDictionary<string, JsonElement>? args, string key)
    {
        if (args is null || !args.TryGetValue(key, out var e))
            return null;
        return e.ValueKind == JsonValueKind.Number && e.TryGetInt64(out var v)
            ? v
            : null;
    }

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

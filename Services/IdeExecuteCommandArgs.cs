using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Общий разбор <c>execute_command</c> / моста веб-портала: merge вложенного <c>args</c> с верхним уровнем (как <see cref="IdeMcpServer"/>).</summary>
public static class IdeExecuteCommandArgs
{
    /// <summary>
    /// Клиенты шлют <c>{ "command_id": "…", "args": { "workspace_path": "…" } }</c> — сливаем вложенный объект с верхним уровнем (верх при конфликте важнее).
    /// </summary>
    public static IReadOnlyDictionary<string, JsonElement>? MergeNestedArgs(IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("args", out var nested) || nested.ValueKind != JsonValueKind.Object)
            return args;
        var merged = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var prop in nested.EnumerateObject())
            merged[prop.Name] = prop.Value;
        foreach (var kv in args)
        {
            if (string.Equals(kv.Key, "args", StringComparison.Ordinal))
                continue;
            merged[kv.Key] = kv.Value;
        }
        return merged;
    }
}

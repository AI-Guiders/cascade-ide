#nullable enable

using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Имена MCP-прокси (<c>ide_*</c>), которые один-в-один дублируются в MAF-агента как именованные <see cref="Microsoft.Extensions.AI.AIFunction"/>
/// Параметры тех же типов что у MCP через JSON-объект (схему даёт MCP-каталог).
/// </summary>
internal static class CascadeIdeMafPromotedTools
{
    /// <summary>Точное совпадение с полем <see cref="IdeMcpToolCatalog.BuildTools"/>/<c>Name</c>.</summary>
    internal static IEnumerable<string> GetPromotedMcpToolNames(bool includeDebugBuildOnlyExtras)
    {
        foreach (var n in NamesCore)
            yield return n;

        if (includeDebugBuildOnlyExtras)
        {
            foreach (var n in DebugOnlyExtras)
                yield return n;
        }
    }

    private static readonly string[] NamesCore =
    [
        "ide_get_ide_state",
        "ide_get_editor_state",
        "ide_get_editor_content_range",
        "ide_get_solution_info",
        "ide_get_solution_files",
        "ide_get_build_output",
        "ide_build",
        "ide_run_tests",
        "ide_run_affected_tests",
        "ide_open_file",
        "ide_load_solution",
        "ide_go_to_position",
        "ide_select",
        "ide_apply_edit",
        "ide_set_breakpoint",
        "ide_remove_breakpoint",
        "ide_get_current_file_diagnostics",
        "ide_ping",
        "ide_search_workspace_text",
        "ide_get_ui_layout",
        "ide_route_context",
        "ide_read_hot_context",
    ];

    private static readonly string[] DebugOnlyExtras =
    [
        "ide_get_debug_snapshot",
        "ide_debug_launch",
        "ide_debug_ping",
        "ide_list_tools",
    ];

    internal static HashSet<string> BuildLookup(bool includeDebugBuildOnlyExtras) =>
        new(GetPromotedMcpToolNames(includeDebugBuildOnlyExtras), StringComparer.Ordinal);

    /// <summary>Маппинг как в <see cref="IdeMcpServer"/> (имя тула MCP → command_id).</summary>
    internal static bool TryMcpProxyToolToCommandId(string mcpToolName, out string commandId)
    {
        commandId = "";
        if (string.IsNullOrEmpty(mcpToolName))
            return false;
        if (!mcpToolName.StartsWith("ide_", StringComparison.Ordinal))
            return false;

        commandId = mcpToolName["ide_".Length..];
        if (string.Equals(commandId, IdeCommands.Build, StringComparison.Ordinal))
            commandId = IdeCommands.BuildStructured;
        return true;
    }

    internal static IReadOnlyDictionary<string, JsonElement>? JsonArgsToDict(JsonElement arguments)
    {
        var kind = arguments.ValueKind;
        if (kind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;
        if (kind != JsonValueKind.Object)
            return null;
        var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var p in arguments.EnumerateObject())
            dict[p.Name] = p.Value;
        return dict;
    }
}

using System.Reflection;

namespace CascadeIDE.Services;

/// <summary>
/// MCP tool names for Cursor: only <c>[A-Za-z0-9_]</c>; combined server+tool length ≤ 60 (conservative cap on tool part).
/// <see cref="IdeCommands"/> <c>command_id</c> may contain <c>.</c> — wire name uses <c>_</c>.
/// </summary>
internal static class IdeMcpToolNaming
{
    /// <summary>Max length of full tool name (<c>ide_*</c>). Leaves room for server names like <c>cascade-ide-debug</c>.</summary>
    public const int MaxToolNameLength = 45;

    private static readonly Dictionary<string, string> CommandIdToToolName;
    private static readonly Dictionary<string, string> ToolNameToCommandId;

    static IdeMcpToolNaming()
    {
        CommandIdToToolName = new Dictionary<string, string>(StringComparer.Ordinal);
        ToolNameToCommandId = new Dictionary<string, string>(StringComparer.Ordinal);

        var type = typeof(IdeCommands);
        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.FieldType != typeof(string))
                continue;
            var commandId = (string?)f.GetValue(null);
            if (string.IsNullOrWhiteSpace(commandId))
                continue;
            Register(commandId);
        }

        // Shorter wire name (combined server+tool ≤ 60 in Cursor).
        RegisterAlias(IdeCommands.ChatToggleProductSpineInAgentContext, "chat_toggle_spine_ctx");
    }

    public static string ToToolName(string commandId)
    {
        if (CommandIdToToolName.TryGetValue(commandId, out var name))
            return name;
        return Register(commandId);
    }

    public static bool TryToCommandId(string toolName, out string commandId)
    {
        if (ToolNameToCommandId.TryGetValue(toolName, out var id))
        {
            commandId = id;
            return true;
        }

        commandId = "";
        return false;
    }

    /// <summary>Skip auto-proxy when no stable short name fits Cursor limits.</summary>
    public static bool IsSupportedAutoProxyToolName(string commandId) =>
        ToToolName(commandId).Length <= MaxToolNameLength;

    private static void RegisterAlias(string commandId, string suffixAfterIde)
    {
        var toolName = "ide_" + suffixAfterIde;
        if (toolName.Length > MaxToolNameLength)
            throw new InvalidOperationException($"MCP tool name too long: {toolName}");

        CommandIdToToolName[commandId] = toolName;
        ToolNameToCommandId[toolName] = commandId;
    }

    private static string Register(string commandId)
    {
        if (CommandIdToToolName.TryGetValue(commandId, out var existing))
            return existing;

        var suffix = commandId.Replace('.', '_');
        var toolName = "ide_" + suffix;
        CommandIdToToolName[commandId] = toolName;
        ToolNameToCommandId[toolName] = commandId;
        return toolName;
    }
}

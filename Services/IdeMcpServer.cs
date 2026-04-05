using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CascadeIDE.Services;

public static class IdeMcpServer
{
    public static McpServerOptions BuildOptions(IIdeMcpActions actions)
    {
        bool includeDebugTools = false;
#if DEBUG
        includeDebugTools = true;
#endif
        var toolsList = IdeMcpToolCatalog.BuildTools(includeDebugTools);

        return new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "CascadeIDE", Version = "0.1.0" },
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities { Tools = new ToolsCapability { ListChanged = false } },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = toolsList }),
                CallToolHandler = async (request, cancellationToken) =>
                {
                    var name = request.Params?.Name ?? "";
                    var args = request.Params?.Arguments is IReadOnlyDictionary<string, JsonElement> a ? a : null;
                    try
                    {
                        var text = await CallToolByConventionAsync(actions, name, args, cancellationToken).ConfigureAwait(false);
                        // Тулы-действия при ошибке возвращают текст (не "OK") — помечаем как IsError, чтобы агент видел сбой.
                        bool isError;
                        if (name == "ide_execute_command")
                            isError = text.StartsWith("Missing", StringComparison.Ordinal) || text.StartsWith("Unknown command", StringComparison.Ordinal) || text.StartsWith("Error", StringComparison.Ordinal);
                        else
                        {
                            var isActionTool = name is "ide_open_file" or "ide_load_solution" or "ide_select" or "ide_set_breakpoint" or "ide_remove_breakpoint"
                                or "ide_show_preview" or "ide_show_editor_preview" or "ide_apply_edit" or "ide_go_to_position" or "ide_focus_editor"
                                or "ide_set_ui_theme" or "ide_set_control_layout" or "ide_set_control_text" or "ide_click_control"
                                or "ide_send_keys" or "ide_set_focus" or "ide_highlight_control" or "ide_set_panel_size" or "ide_add_control"
                                or "ide_show_breakpoints" or "ide_show_debug_position" or "ide_show_debug_state" or "ide_write_agent_notes"
                                or "ide_run_code_cleanup" or "ide_git_commit" or "ide_git_push"
                                or "ide_git_log" or "ide_git_fetch" or "ide_git_pull" or "ide_git_branch" or "ide_git_show" or "ide_git_submodule";
                            isError = isActionTool && text != "OK";
                        }
                        return new CallToolResult { Content = [new TextContentBlock { Text = text }], IsError = isError };
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("invalid thread", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var dir = AppContext.BaseDirectory;
                                var logPath = Path.Combine(dir, "invalid-thread-log.txt");
                                File.WriteAllText(logPath, ex.ToString());
                            }
                            catch { /* ignore */ }
#if DEBUG
                            var stack = ex.StackTrace ?? "";
                            return new CallToolResult { Content = [new TextContentBlock { Text = "Error: " + ex.Message + " [caught in IdeMcpServer]\n\n" + stack }], IsError = true };
#endif
                        }
                        return new CallToolResult { Content = [new TextContentBlock { Text = "Error: " + ex.Message }], IsError = true };
                    }
                }
            }
        };
    }

    private static async Task<string> CallToolByConventionAsync(
        IIdeMcpActions actions,
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? args,
        CancellationToken cancellationToken)
    {
        // Special case: dispatcher tool supports arbitrary command ids and nested args object.
        if (toolName == "ide_execute_command")
            return await CallExecuteCommand(actions, args, cancellationToken).ConfigureAwait(false);

        if (!toolName.StartsWith("ide_", StringComparison.Ordinal))
            return $"Unknown tool: {toolName}";

        // Most proxy tools map 1:1:
        //   tool ide_open_file -> command_id open_file
        //   tool ide_get_ui_theme -> command_id get_ui_theme
        // and so on.
        var commandId = toolName["ide_".Length..];

        // Compatibility overrides (tool name stays stable; backend command id may evolve).
        if (string.Equals(commandId, IdeCommands.Build, StringComparison.Ordinal))
            commandId = IdeCommands.BuildStructured;

        return await actions.ExecuteCommandAsync(commandId, args, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> CallExecuteCommand(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        var merged = MergeExecuteCommandArgs(args);
        var commandId = merged is not null && merged.TryGetValue("command_id", out var cid) ? cid.GetString() : null;
        if (string.IsNullOrEmpty(commandId))
            return "Missing command_id";
        return await actions.ExecuteCommandAsync(commandId, merged, cancellationToken);
    }

    /// <summary>
    /// Клиенты MCP часто шлют <c>{ "command_id": "…", "args": { "workspace_path": "…" } }</c> — сливаем вложенный объект с верхним уровнем (верхний уровень при конфликте важнее).
    /// </summary>
    private static IReadOnlyDictionary<string, JsonElement>? MergeExecuteCommandArgs(IReadOnlyDictionary<string, JsonElement>? args)
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

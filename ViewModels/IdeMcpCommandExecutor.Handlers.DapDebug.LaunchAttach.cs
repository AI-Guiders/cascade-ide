namespace CascadeIDE.ViewModels;

/// <summary>MCP DAP: ping, launch и attach.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDapDebugLaunchAttach(Action<string, Handler> add)
    {
        add(Services.IdeCommands.DebugPing, async (_, _) => await Task.FromResult(Services.IdeDapDebugSession.Ping()));

        add(Services.IdeCommands.DebugLaunch, async (args, ct) =>
        {
            var ws = McpCommandJsonArgs.String(args, "workspace_path");
            var target = McpCommandJsonArgs.String(args, "target_path");
            if (string.IsNullOrWhiteSpace(ws))
                return await _vm.DebugLaunchInteractiveAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(target))
            {
                try
                {
                    return await _vm.DebugLaunchByProfileOrResolvedTargetAsync(
                        ws!,
                        targetPath: null,
                        McpCommandJsonArgs.String(args, "profile_name"),
                        McpCommandJsonArgs.String(args, "netcoredbg_path"),
                        McpCommandJsonArgs.StringList(args, "program_args"),
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return "# Error: " + ex.Message;
                }
            }

            try
            {
                return await _vm.DapDebug.LaunchAsync(
                    ws!,
                    target!,
                    McpCommandJsonArgs.String(args, "netcoredbg_path"),
                    McpCommandJsonArgs.StringList(args, "program_args"),
                    environment: null,
                    workingDirectoryOverride: null,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return "# Error: " + ex.Message;
            }
        });

        add(Services.IdeCommands.DebugAttach, async (args, ct) =>
        {
            var ws = McpCommandJsonArgs.String(args, "workspace_path");
            if (string.IsNullOrWhiteSpace(ws))
                return "workspace_path is required.";
            if (args is null || !args.TryGetValue("process_id", out var pidEl) || !pidEl.TryGetInt32(out var pid) || pid <= 0)
                return "process_id (positive integer) is required.";
            try
            {
                return await _vm.DapDebug.AttachAsync(
                    ws!,
                    pid,
                    McpCommandJsonArgs.String(args, "target_path"),
                    McpCommandJsonArgs.String(args, "netcoredbg_path"),
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return "# Error: " + ex.Message;
            }
        });
    }
}

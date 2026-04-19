using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>DAP / отладка.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDapDebug(Action<string, Handler> add)
    {
        add(Services.IdeCommands.DebugPing, async (_, _) => await Task.FromResult(Services.IdeDapDebugSession.Ping()));

        add(Services.IdeCommands.DebugLaunch, async (args, ct) =>
        {
            var ws = McpCommandJsonArgs.String(args, "workspace_path");
            var target = McpCommandJsonArgs.String(args, "target_path");
            if (string.IsNullOrWhiteSpace(ws) || string.IsNullOrWhiteSpace(target))
                return "workspace_path and target_path are required.";
            try
            {
                return await _vm.DapDebug.LaunchAsync(
                    ws!,
                    target!,
                    McpCommandJsonArgs.String(args, "netcoredbg_path"),
                    McpCommandJsonArgs.StringList(args, "program_args"),
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

        add(Services.IdeCommands.DebugContinue, async (_, ct) =>
        {
            try { return await _vm.DapDebug.ContinueAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.DebugStepOver, async (_, ct) =>
        {
            try { return await _vm.DapDebug.StepOverAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.DebugStepInto, async (_, ct) =>
        {
            try { return await _vm.DapDebug.StepIntoAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.DebugStepOut, async (_, ct) =>
        {
            try { return await _vm.DapDebug.StepOutAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.DebugStop, async (_, ct) =>
        {
            try { return await _vm.DapDebug.StopAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.DebugStackTrace, async (_, ct) =>
        {
            try { return await _vm.DapDebug.StackTraceAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.DebugVariables, async (args, ct) =>
        {
            var frameIndex = McpCommandJsonArgs.Int(args, "frame_index", 0);
            try { return await _vm.DapDebug.VariablesAsync(frameIndex, ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });
    }
}

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP DAP: шагание, стоп, стек, снимок, переменные кадра.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDapDebugStepping(Action<string, Handler> add)
    {
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
            try { return await _vm.DapDebug.StackTraceFromSnapshotAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });

        add(Services.IdeCommands.GetDebugSnapshot, async (_, ct) =>
        {
            try { return await _actions.GetDebugSnapshotAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# Error: " + ex.Message; }
        });

        add(Services.IdeCommands.DebugVariables, async (args, ct) =>
        {
            var frameIndex = McpCommandJsonArgs.Int(args, "frame_index", 0);
            try { return await _vm.DapDebug.VariablesAsync(frameIndex, ct).ConfigureAwait(false); }
            catch (Exception ex) { return "# " + ex.Message; }
        });
    }
}

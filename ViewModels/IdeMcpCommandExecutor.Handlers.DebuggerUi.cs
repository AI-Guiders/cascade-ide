using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Поверхность отладки.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDebuggerBreakpoints(Action<string, Handler> add)
    {
        add(Services.IdeCommands.SetBreakpoint, async (args, ct) =>
        {
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path")) || !args.TryGetValue("line", out _))
                return "Missing file_path or line";
            return await _vm.CompleteMcpSetBreakpointAsync(
                McpCommandJsonArgs.String(args, "file_path")!,
                McpCommandJsonArgs.Int(args, "line", 1),
                McpCommandJsonArgs.String(args, "condition"),
                ct).ConfigureAwait(false);
        });
        add(Services.IdeCommands.RemoveBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            a.RemoveBreakpoint(McpCommandJsonArgs.String(args, "file_path")!, McpCommandJsonArgs.Int(args, "line", 1));
            return "OK";
        });
    }

    private void RegisterPreviewAndConfirmation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowPreview, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowPreview(McpCommandJsonArgs.String(args, "title") ?? "", McpCommandJsonArgs.String(args, "content") ?? "");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ShowEditorPreview, async (_, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowEditorPreview();
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.RequestConfirmation, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.RequestConfirmationAsync(McpCommandJsonArgs.String(args, "message") ?? "", ct);
        });
    }

    private void RegisterDebugUiSurface(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowBreakpoints, async (args, _) => await Task.FromResult(ParseAndShowDebugBreakpoints((IIdeMcpActions)_vm, args)));
        add(Services.IdeCommands.ShowDebugPosition, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowDebugPosition(McpCommandJsonArgs.String(args, "file_path"), McpCommandJsonArgs.Int(args, "line"));
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ShowDebugState, async (args, _) => await Task.FromResult(ParseAndShowDebugState((IIdeMcpActions)_vm, args)));

#if DEBUG
        add(Services.IdeCommands.AddControl, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.AddControlAsync(McpCommandJsonArgs.String(args, "parent_name") ?? "", McpCommandJsonArgs.String(args, "control_type") ?? "", McpCommandJsonArgs.String(args, "content"), McpCommandJsonArgs.String(args, "name"));
        });
#endif
    }
}
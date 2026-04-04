using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDebuggerBreakpoints(Action<string, Handler> add)
    {
        add(Services.IdeCommands.SetBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(JsonArgs.String(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            a.SetBreakpoint(JsonArgs.String(args, "file_path")!, JsonArgs.Int(args, "line", 1), JsonArgs.String(args, "condition"));
            return "OK";
        });
        add(Services.IdeCommands.RemoveBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(JsonArgs.String(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            a.RemoveBreakpoint(JsonArgs.String(args, "file_path")!, JsonArgs.Int(args, "line", 1));
            return "OK";
        });
    }

    private void RegisterPreviewAndConfirmation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowPreview, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowPreview(JsonArgs.String(args, "title") ?? "", JsonArgs.String(args, "content") ?? "");
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
            return await a.RequestConfirmationAsync(JsonArgs.String(args, "message") ?? "", ct);
        });
    }

    private void RegisterDebugUiSurface(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowBreakpoints, async (args, _) => await Task.FromResult(ParseAndShowDebugBreakpoints((IIdeMcpActions)_vm, args)));
        add(Services.IdeCommands.ShowDebugPosition, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowDebugPosition(JsonArgs.String(args, "file_path"), JsonArgs.Int(args, "line"));
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ShowDebugState, async (args, _) => await Task.FromResult(ParseAndShowDebugState((IIdeMcpActions)_vm, args)));

#if DEBUG
        add(Services.IdeCommands.AddControl, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.AddControlAsync(JsonArgs.String(args, "parent_name") ?? "", JsonArgs.String(args, "control_type") ?? "", JsonArgs.String(args, "content"), JsonArgs.String(args, "name"));
        });
#endif
    }
}
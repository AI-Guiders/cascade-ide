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
            if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            a.SetBreakpoint(S(args, "file_path")!, I(args, "line", 1), S(args, "condition"));
            return "OK";
        });
        add(Services.IdeCommands.RemoveBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            a.RemoveBreakpoint(S(args, "file_path")!, I(args, "line", 1));
            return "OK";
        });
    }

    private void RegisterPreviewAndConfirmation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowPreview, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowPreview(S(args, "title") ?? "", S(args, "content") ?? "");
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
            return await a.RequestConfirmationAsync(S(args, "message") ?? "", ct);
        });
    }

    private void RegisterDebugUiSurface(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowBreakpoints, async (args, _) => await Task.FromResult(ParseAndShowDebugBreakpoints((IIdeMcpActions)_vm, args)));
        add(Services.IdeCommands.ShowDebugPosition, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowDebugPosition(S(args, "file_path"), I(args, "line"));
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ShowDebugState, async (args, _) => await Task.FromResult(ParseAndShowDebugState((IIdeMcpActions)_vm, args)));

#if DEBUG
        add(Services.IdeCommands.AddControl, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.AddControlAsync(S(args, "parent_name") ?? "", S(args, "control_type") ?? "", S(args, "content"), S(args, "name"));
        });
#endif
    }
}
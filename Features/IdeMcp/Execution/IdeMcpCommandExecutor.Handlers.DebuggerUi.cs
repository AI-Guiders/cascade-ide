using System.Text.Json;
using CascadeIDE.Models.Editor;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>Поверхность отладки.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDebuggerBreakpoints(Action<string, Handler> add)
    {
        add(Services.IdeCommands.SetBreakpoint, async (args, ct) =>
        {
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path or line";
            if (!tryParseBreakpointLine(args, out var line, out var lineErr))
                return lineErr;
            return await _vm.CompleteMcpSetBreakpointAsync(
                McpCommandJsonArgs.String(args, "file_path")!,
                line.Value,
                McpCommandJsonArgs.String(args, "condition"),
                ct).ConfigureAwait(false);
        });
        add(Services.IdeCommands.RemoveBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path"))) return "Missing file_path or line";
            if (!tryParseBreakpointLine(args, out var line, out var lineErr))
                return lineErr;
            a.RemoveBreakpoint(McpCommandJsonArgs.String(args, "file_path")!, line.Value);
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
#if DEBUG
        add(Services.IdeCommands.AddControl, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.AddControlAsync(McpCommandJsonArgs.String(args, "parent_name") ?? "", McpCommandJsonArgs.String(args, "control_type") ?? "", McpCommandJsonArgs.String(args, "content"), McpCommandJsonArgs.String(args, "name"));
        });
#endif
    }

    private static bool tryParseBreakpointLine(IReadOnlyDictionary<string, JsonElement> args, out LineNumber line, out string error)
    {
        line = default;
        error = "";
        if (!args.TryGetValue("line", out var el) || el.ValueKind != JsonValueKind.Number || !el.TryGetInt32(out var raw))
        {
            error = "Missing or invalid line (ожидается целое число).";
            return false;
        }

        if (!LineNumber.TryCreate(raw, out line))
        {
            error = $"Invalid line: ожидается ≥ {LineNumber.MinimumOneBasedInclusive}, получено {raw}.";
            return false;
        }

        return true;
    }
}
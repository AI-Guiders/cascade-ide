using CascadeIDE.Models.Editor;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: правка текста и переход к позиции в файле.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterEditAndNavigation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ApplyEdit, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || !args.TryGetValue("new_text", out _)) return "Missing arguments";
            if (!EditorTextSpan.TryParse(args, out var span, out var errSpan))
                return errSpan;
            a.ApplyEdit(span.File.Value, span.StartLine.Value, span.StartColumn.Value, span.EndLine.Value, span.EndColumn.Value, McpCommandJsonArgs.String(args, "new_text") ?? "");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.GoToPosition, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (!EditorGoToPositionMcpArgs.TryParse(args, out var file, out var line, out var column, out var endLine, out var endColumn, out var err))
                return err;
            a.GoToPosition(file.Value, line.Value, column.Value, endLine?.Value, endColumn?.Value);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.RevealEditorRange, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (!EditorRevealRangeMcpArgs.TryParse(args, out var file, out var lines, out var err))
                return err;
            a.RevealEditorRange(file.Value, lines.Start.Value, lines.End.Value);
            return await Task.FromResult("OK");
        });
    }
}

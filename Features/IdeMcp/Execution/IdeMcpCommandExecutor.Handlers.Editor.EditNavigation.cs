using CascadeIDE.Models.Editor;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP: правка текста и переход к позиции в файле.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterEditAndNavigation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ApplyEdit, async (args, ct) =>
        {
            var a = _actions;
            if (args is null || !args.TryGetValue("new_text", out _)) return "Missing arguments";
            if (!EditorTextSpan.TryParse(args, out var span, out var errSpan))
                return errSpan;
            return await a.ApplyEditAsync(
                span.File.Value,
                span.StartLine.Value,
                span.StartColumn.Value,
                span.EndLine.Value,
                span.EndColumn.Value,
                McpCommandJsonArgs.String(args, "new_text") ?? "");
        });
        add(Services.IdeCommands.GoToPosition, async (args, ct) =>
        {
            var a = _actions;
            if (!EditorGoToPositionMcpArgs.TryParse(args, out var file, out var line, out var column, out var endLine, out var endColumn, out var err))
                return err;
            a.GoToPosition(file.Value, line.Value, column.Value, endLine?.Value, endColumn?.Value);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.RevealEditorRange, async (args, ct) =>
        {
            var a = _actions;
            if (!EditorRevealRangeMcpArgs.TryParse(args, out var request, out var err))
                return err;

            var workspaceRoot = TryGetWorkspaceRoot(a);
            var solutionPath = TryGetAttachSolutionPath();
            if (!EditorRevealRangeResolution.TryResolveLines(
                    request,
                    workspaceRoot,
                    out var lines,
                    out var detail,
                    out var usedFallback,
                    solutionPath))
                return $"reveal failed: {detail}";

            a.RevealEditorRange(request.File.Value, lines.Start.Value, lines.End.Value, request.DurationMs);
            var suffix = usedFallback ? $" (fallback: {detail})" : string.IsNullOrEmpty(detail) ? "" : $" ({detail})";
            return await Task.FromResult($"OK lines={lines.Start.Value}-{lines.End.Value}{suffix}");
        });
    }
}

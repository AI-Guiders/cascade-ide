using System.Text.Json;

using CascadeIDE.Models.Editor;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP: состояние редактора, диапазон текста и текст открытого документа.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterEditorStateAndContent(Action<string, Handler> add)
    {
        add(Services.IdeCommands.GetEditorState, async (args, _) =>
        {
            var a = _actions;
            return await a.GetEditorStateAsync(args is not null && args.TryGetValue("max_preview_chars", out var mpc) && mpc.TryGetInt32(out var maxPreview) ? maxPreview : null);
        });
        add(Services.IdeCommands.GetEditorContentRange, async (args, _) =>
        {
            var a = _actions;
            if (!EditorContentLineRangeMcpArgs.TryParse(args, out var lines, out var errLines))
                return errLines;
            return await a.GetEditorContentRangeAsync(lines.Start.Value, lines.End.Value);
        });
        add(Services.IdeCommands.GetOpenDocumentText, async (args, _) =>
        {
            var a = _actions;
            int? maxCharsOpen = null;
            if (args is not null && args.TryGetValue("max_chars", out var mco) && mco.ValueKind == JsonValueKind.Number && mco.TryGetInt32(out var mcOpen) && mcOpen > 0)
                maxCharsOpen = mcOpen;
            return await a.GetOpenDocumentTextAsync(McpCommandJsonArgs.String(args, "file_path"), maxCharsOpen);
        });
        add(Services.IdeCommands.ReadWorkspaceFile, async (args, _) =>
        {
            var a = _actions;
            var path = McpCommandJsonArgs.String(args, "file_path");
            if (string.IsNullOrWhiteSpace(path))
                return """{"error":"no_path","message":"file_path is required."}""";
            int? offset = null;
            int? limit = null;
            int? maxChars = null;
            if (args is not null)
            {
                if (args.TryGetValue("offset", out var off) && off.TryGetInt32(out var o) && o > 0)
                    offset = o;
                if (args.TryGetValue("limit", out var lim) && lim.TryGetInt32(out var l) && l > 0)
                    limit = l;
                if (args.TryGetValue("max_chars", out var mc) && mc.TryGetInt32(out var m) && m > 0)
                    maxChars = m;
            }

            return await a.ReadWorkspaceFileAsync(path, offset, limit, maxChars);
        });
        add(Services.IdeCommands.SaveDocument, async (args, _) =>
            await _actions.SaveDocumentAsync(
                McpCommandJsonArgs.String(args, "file_path"),
                McpCommandJsonArgs.String(args, "content")));
    }
}

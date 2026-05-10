using System.Text.Json;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: состояние редактора, диапазон текста и текст открытого документа.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterEditorStateAndContent(Action<string, Handler> add)
    {
        add(Services.IdeCommands.GetEditorState, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.GetEditorStateAsync(args is not null && args.TryGetValue("max_preview_chars", out var mpc) && mpc.TryGetInt32(out var maxPreview) ? maxPreview : null);
        });
        add(Services.IdeCommands.GetEditorContentRange, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.GetEditorContentRangeAsync(McpCommandJsonArgs.Int(args, "start_line", 1), McpCommandJsonArgs.Int(args, "end_line", 1));
        });
        add(Services.IdeCommands.GetOpenDocumentText, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            int? maxCharsOpen = null;
            if (args is not null && args.TryGetValue("max_chars", out var mco) && mco.ValueKind == JsonValueKind.Number && mco.TryGetInt32(out var mcOpen) && mcOpen > 0)
                maxCharsOpen = mcOpen;
            return await a.GetOpenDocumentTextAsync(McpCommandJsonArgs.String(args, "file_path"), maxCharsOpen);
        });
    }
}

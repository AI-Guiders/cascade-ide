using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Редактор.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterCore(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ListTools, async (_, _) =>
        {
            bool includeDebugTools = false;
#if DEBUG
            includeDebugTools = true;
#endif
            var tools = Services.IdeMcpToolCatalog.BuildTools(includeDebugTools);
            return await Task.FromResult(JsonSerializer.Serialize(tools));
        });
    }

    private void RegisterEditorAndSolution(Action<string, Handler> add)
    {
        add(Services.IdeCommands.OpenFile, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "path"))) return "Missing path";
            a.OpenFile(McpCommandJsonArgs.String(args, "path")!);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.LoadSolution, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "path"))) return "Missing path";
            a.LoadSolution(McpCommandJsonArgs.String(args, "path")!);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.Select, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path"))) return "Missing file_path";
            a.SelectInEditor(McpCommandJsonArgs.String(args, "file_path"), McpCommandJsonArgs.Int(args, "start_line"), McpCommandJsonArgs.Int(args, "start_column"), McpCommandJsonArgs.Int(args, "end_line"), McpCommandJsonArgs.Int(args, "end_column"));
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ChatSelectMessage, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || !args.TryGetValue("index", out var raw) || !raw.TryGetInt32(out var index))
                return "Missing index";
            return await a.SelectChatMessageAsync(index);
        });
        add(Services.IdeCommands.ChatGetSelectedMessage, async (_, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.GetSelectedChatMessageAsync();
        });
        add(Services.IdeCommands.ChatEditMessage, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "message_id")))
                return "Missing message_id";
            if (args is null || !args.TryGetValue("new_content", out _))
                return "Missing new_content";
            _ = ct;
            return await a.EditChatAssistantMessageAsync(
                McpCommandJsonArgs.String(args, "message_id")!,
                McpCommandJsonArgs.String(args, "new_content") ?? "",
                McpCommandJsonArgs.String(args, "reason"));
        });
        add(Services.IdeCommands.ChatExportReadable, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            _ = ct;
            var write = McpCommandJsonArgs.Bool(args, "write_file");
            return await a.ExportChatReadableAsync(write, McpCommandJsonArgs.String(args, "file_name"));
        });
    }

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

    private void RegisterEditAndNavigation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ApplyEdit, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path")) || !args.TryGetValue("new_text", out _)) return "Missing arguments";
            a.ApplyEdit(McpCommandJsonArgs.String(args, "file_path")!, McpCommandJsonArgs.Int(args, "start_line"), McpCommandJsonArgs.Int(args, "start_column"), McpCommandJsonArgs.Int(args, "end_line"), McpCommandJsonArgs.Int(args, "end_column"), McpCommandJsonArgs.String(args, "new_text") ?? "");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.GoToPosition, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(McpCommandJsonArgs.String(args, "file_path")) || !args.TryGetValue("line", out _) || !args.TryGetValue("column", out _)) return "Missing file_path, line or column";
            int? endLine = args.TryGetValue("end_line", out var el) && el.TryGetInt32(out var endL) ? endL : null;
            int? endCol = args.TryGetValue("end_column", out var ec) && ec.TryGetInt32(out var endC) ? endC : null;
            a.GoToPosition(McpCommandJsonArgs.String(args, "file_path"), McpCommandJsonArgs.Int(args, "line"), McpCommandJsonArgs.Int(args, "column"), endLine, endCol);
            return await Task.FromResult("OK");
        });
    }
}

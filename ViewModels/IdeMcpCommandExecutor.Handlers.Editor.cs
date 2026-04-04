using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

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
            if (string.IsNullOrEmpty(JsonArgs.String(args, "path"))) return "Missing path";
            a.OpenFile(JsonArgs.String(args, "path")!);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.LoadSolution, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (string.IsNullOrEmpty(JsonArgs.String(args, "path"))) return "Missing path";
            a.LoadSolution(JsonArgs.String(args, "path")!);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.Select, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(JsonArgs.String(args, "file_path"))) return "Missing file_path";
            a.SelectInEditor(JsonArgs.String(args, "file_path"), JsonArgs.Int(args, "start_line"), JsonArgs.Int(args, "start_column"), JsonArgs.Int(args, "end_line"), JsonArgs.Int(args, "end_column"));
            return await Task.FromResult("OK");
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
            return await a.GetEditorContentRangeAsync(JsonArgs.Int(args, "start_line", 1), JsonArgs.Int(args, "end_line", 1));
        });
        add(Services.IdeCommands.GetOpenDocumentText, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            int? maxCharsOpen = null;
            if (args is not null && args.TryGetValue("max_chars", out var mco) && mco.ValueKind == JsonValueKind.Number && mco.TryGetInt32(out var mcOpen) && mcOpen > 0)
                maxCharsOpen = mcOpen;
            return await a.GetOpenDocumentTextAsync(JsonArgs.String(args, "file_path"), maxCharsOpen);
        });
    }

    private void RegisterEditAndNavigation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ApplyEdit, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(JsonArgs.String(args, "file_path")) || !args.TryGetValue("new_text", out _)) return "Missing arguments";
            a.ApplyEdit(JsonArgs.String(args, "file_path")!, JsonArgs.Int(args, "start_line"), JsonArgs.Int(args, "start_column"), JsonArgs.Int(args, "end_line"), JsonArgs.Int(args, "end_column"), JsonArgs.String(args, "new_text") ?? "");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.GoToPosition, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(JsonArgs.String(args, "file_path")) || !args.TryGetValue("line", out _) || !args.TryGetValue("column", out _)) return "Missing file_path, line or column";
            int? endLine = args.TryGetValue("end_line", out var el) && el.TryGetInt32(out var endL) ? endL : null;
            int? endCol = args.TryGetValue("end_column", out var ec) && ec.TryGetInt32(out var endC) ? endC : null;
            a.GoToPosition(JsonArgs.String(args, "file_path"), JsonArgs.Int(args, "line"), JsonArgs.Int(args, "column"), endLine, endCol);
            return await Task.FromResult("OK");
        });
    }
}

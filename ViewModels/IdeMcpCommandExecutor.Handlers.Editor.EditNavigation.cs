namespace CascadeIDE.ViewModels;

/// <summary>MCP: правка текста и переход к позиции в файле.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
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

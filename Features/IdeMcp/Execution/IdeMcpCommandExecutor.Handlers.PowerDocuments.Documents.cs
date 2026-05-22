namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP-хендлеры вкладок документов: переоткрытие, активация, закрепление, перенос по группам редакторов.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDocuments(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ReopenClosedDocument, async (_, _) =>
        {
            if (_vm.ReopenClosedDocumentCommand.CanExecute(null))
                _vm.ReopenClosedDocumentCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ActivateDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var pathAct = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.ActivateDocumentCommand.CanExecute(pathAct))
                _vm.ActivateDocumentCommand.Execute(pathAct);
            return "OK";
        });
        add(Services.IdeCommands.CloseDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var pathClose = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.CloseDocumentCommand.CanExecute(pathClose))
                _vm.CloseDocumentCommand.Execute(pathClose);
            return "OK";
        });
        add(Services.IdeCommands.TogglePinDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var pathPin = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.TogglePinDocumentCommand.CanExecute(pathPin))
                _vm.TogglePinDocumentCommand.Execute(pathPin);
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup1, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var p1 = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.MoveDocumentToGroup1Command.CanExecute(p1))
                _vm.MoveDocumentToGroup1Command.Execute(p1);
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup2, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var p2 = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.MoveDocumentToGroup2Command.CanExecute(p2))
                _vm.MoveDocumentToGroup2Command.Execute(p2);
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup3, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var p3 = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.MoveDocumentToGroup3Command.CanExecute(p3))
                _vm.MoveDocumentToGroup3Command.Execute(p3);
            return "OK";
        });
    }
}

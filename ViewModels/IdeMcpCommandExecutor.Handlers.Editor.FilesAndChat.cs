using System.Text.Json;

using CascadeIDE.Models.Editor;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: открытие файла, загрузка решения, выделение в редакторе, выбор/редактирование сообщений чата.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
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
        add(Services.IdeCommands.CreateProjectInSolution, async (args, ct) =>
        {
            var template = McpCommandJsonArgs.String(args, "template");
            var projectName = McpCommandJsonArgs.String(args, "project_name");
            if (string.IsNullOrWhiteSpace(template))
                return "Missing template";
            if (string.IsNullOrWhiteSpace(projectName))
                return "Missing project_name";
            return await _vm.TryCreateProjectInSolutionAsync(template!, projectName!, ct).ConfigureAwait(false);
        });
        add(Services.IdeCommands.Select, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (!EditorTextSpan.TryParse(args, out var span, out var err))
                return err;
            a.SelectInEditor(span.File.Value, span.StartLine.Value, span.StartColumn.Value, span.EndLine.Value, span.EndColumn.Value);
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
    }
}

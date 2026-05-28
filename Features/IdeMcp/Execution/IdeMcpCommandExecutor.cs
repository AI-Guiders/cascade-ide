using System.Text.Json;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>Диспетчер MCP-команд IDE: разбор args и вызов <see cref="IIdeMcpActions"/> / UI-команд главного окна.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private readonly IMainWindowMcpHostContext _host;
    private readonly MainWindowViewModel _vm;
    private readonly IIdeMcpActions _actions;
    private readonly Dictionary<string, Handler> _handlers;

    private delegate Task<string> Handler(IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken);

    public IdeMcpCommandExecutor(IMainWindowMcpHostContext host, IIdeMcpActions actions)
    {
        _host = host;
        _vm = host.Vm;
        _actions = actions;
        _handlers = BuildHandlers();
    }

    /// <summary>Вход с MCP/агента маршалится на UI в <see cref="MainWindowViewModel"/> до вызова хендлеров; UI-операции выполнять напрямую без вложенного маршалинга.</summary>
    public async Task<string> ExecuteAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        var shellBlock = ShellEscapePolicy.TryBlockJson(
            commandId,
            _vm.GetCascadeSettingsForExecutor().Agent.Environment.ShellEscapeTier);
        if (shellBlock is not null)
            return shellBlock;

        if (_handlers.TryGetValue(commandId, out var handler))
            return await handler(args, cancellationToken);

        return $"Unknown command: {commandId}";

    }

    private Dictionary<string, Handler> BuildHandlers()
    {
        var map = new Dictionary<string, Handler>(StringComparer.Ordinal);

        void Add(string id, Handler h) => map.Add(id, h);

        RegisterCore(Add);
        RegisterGenerated(Add); // generated .g.cs adds pass-through handlers
        RegisterEditorAndSolution(Add);
        RegisterDebuggerBreakpoints(Add);
        RegisterDapDebug(Add);
        RegisterPreviewAndConfirmation(Add);
        RegisterEditorStateAndContent(Add);
        RegisterEditAndNavigation(Add);
        RegisterIntercom(Add);
        RegisterOutputAndFocus(Add);
        // NOTE: these are now generated:
        // - workspace/solution info
        // - build/tests
        // - git
        // - output/build panel (toggle panels); focus_editor is registered above.
        RegisterUiVisibilityAndModes(Add);
        RegisterMenuAndToolbarCommands(Add);
        RegisterFocusPowerAndAgentActions(Add);
        RegisterDocuments(Add);
        RegisterCorrespondence(Add);
        // NOTE: UI inspection/control (pure IIdeMcpActions) is generated.
        RegisterDebugUiSurface(Add);
        RegisterAgentNotes(Add);
        RegisterAgentEnvironment(Add);

        return map;
    }

    // Generation hook: a source generator (or ProtocolDocGen) can emit a partial method
    // that registers 1:1 handlers for IIdeMcpActions methods.
    partial void RegisterGenerated(Action<string, Handler> add);

    private void RegisterCorrespondence(Action<string, Handler> add)
    {
        add(Services.IdeCommands.OpenWorkspaceAdrCorrespondence, async (_, _) =>
        {
            if (_vm.OpenWorkspaceAdrCorrespondenceCommand.CanExecute(null))
                _vm.OpenWorkspaceAdrCorrespondenceCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.OpenWorkspaceFeatureDocs, async (_, _) =>
        {
            if (_vm.OpenWorkspaceFeatureDocsCommand.CanExecute(null))
                await _vm.OpenWorkspaceFeatureDocsCommand.ExecuteAsync(null);
            return "OK";
        });

        add(Services.IdeCommands.OpenDocsTemplate, async (args, _) =>
        {
            var p = McpCommandJsonArgs.String(args, "path");
            if (_vm.OpenDocsTemplateCommand.CanExecute(p))
                await _vm.OpenDocsTemplateCommand.ExecuteAsync(p);
            return "OK";
        });
    }
}

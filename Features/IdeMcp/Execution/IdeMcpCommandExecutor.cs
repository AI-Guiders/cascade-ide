using System.Text.Json;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>Диспетчер MCP-команд IDE: разбор args и вызов <see cref="IIdeMcpActions"/> / UI-команд главного окна.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private readonly MainWindowViewModel _vm;
    private readonly IIdeMcpActions _actions;
    private readonly Dictionary<string, Handler> _handlers;

    private delegate Task<string> Handler(IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken);

    public IdeMcpCommandExecutor(MainWindowViewModel vm, IIdeMcpActions actions)
    {
        _vm = vm;
        _actions = actions;
        _handlers = BuildHandlers();
    }

    /// <summary>Вход с MCP/агента маршалится на UI в <see cref="MainWindowViewModel"/> до вызова хендлеров; UI-операции выполнять напрямую без вложенного маршалинга.</summary>
    public async Task<string> ExecuteAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
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
        // NOTE: UI inspection/control (pure IIdeMcpActions) is generated.
        RegisterDebugUiSurface(Add);
        RegisterAgentNotes(Add);
        RegisterAgentEnvironment(Add);

        return map;
    }

    // Generation hook: a source generator (or ProtocolDocGen) can emit a partial method
    // that registers 1:1 handlers for IIdeMcpActions methods.
    partial void RegisterGenerated(Action<string, Handler> add);
}

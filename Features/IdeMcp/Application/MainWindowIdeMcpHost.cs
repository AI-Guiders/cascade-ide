using System.Text.Json;
using CascadeIDE.Features.IdeMcp.Execution;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Реализация <see cref="IIdeMcpActions"/> вне <see cref="MainWindowViewModel"/> (Wave 2 Big Bang).
/// Доступ к UI-состоянию — через <see cref="Host"/>; маршалинг команд — на UI-поток.
/// </summary>
internal sealed partial class MainWindowIdeMcpHost : IIdeMcpActions
{
    private readonly MainWindowViewModel _host;
    private readonly IdeMcpCommandExecutor _executor;

    internal MainWindowViewModel Host => _host;

    internal IdeMcpCommandExecutor Executor => _executor;

    internal MainWindowIdeMcpHost(MainWindowViewModel host)
    {
        _host = host;
        _executor = new IdeMcpCommandExecutor(host, this); // host: IMainWindowMcpHostContext
    }

    public Task<string> ExecuteCommandAsync(
        string commandId,
        IReadOnlyDictionary<string, JsonElement>? args,
        CancellationToken cancellationToken = default)
    {
        // AEE: не блокировать MCP на очереди UI (warm-up/HCI). Сервис потокобезопасен.
        if (IsAgentEnvironmentCommand(commandId))
            return _executor.ExecuteAsync(commandId, args, cancellationToken);

        return UiScheduler.Default.InvokeAsync(() => _executor.ExecuteAsync(commandId, args, cancellationToken));
    }

    private static bool IsAgentEnvironmentCommand(string commandId) =>
        commandId is IdeCommands.IdeAgentVerify
            or IdeCommands.IdeAgentVerifyBatch
            or IdeCommands.IdeAgentCancel
            or IdeCommands.IdeAgentStatus
            or IdeCommands.IdeAgentLast
            or IdeCommands.IdeAgentSandboxPrepare;
}

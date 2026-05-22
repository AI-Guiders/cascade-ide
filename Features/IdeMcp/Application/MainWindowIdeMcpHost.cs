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
        _executor = new IdeMcpCommandExecutor(host, this);
    }

    public Task<string> ExecuteCommandAsync(
        string commandId,
        IReadOnlyDictionary<string, JsonElement>? args,
        CancellationToken cancellationToken = default) =>
        UiScheduler.Default.InvokeAsync(() => _executor.ExecuteAsync(commandId, args, cancellationToken));
}

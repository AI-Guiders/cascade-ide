#nullable enable

using CascadeIDE.Features.Intercom.Transport;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private IntercomTransportCoordinator? _intercomTransport;

    public void SetIntercomTransportCoordinator(IntercomTransportCoordinator coordinator)
    {
        _intercomTransport = coordinator;
        coordinator.Configure(
            () => _getCascadeSettings?.Invoke() ?? new Models.CascadeIdeSettings(),
            ResolveAttachWorkspaceRoot,
            () => _sessionId,
            () => _sessionStore,
            ReloadIntercomSessionFromDiskAsync);
    }

    private Func<Models.CascadeIdeSettings>? _getCascadeSettings;

    public void SetCascadeSettingsAccessor(Func<Models.CascadeIdeSettings> getSettings) =>
        _getCascadeSettings = getSettings;

    public async Task StartIntercomTransportAsync(CancellationToken ct = default)
    {
        if (_intercomTransport is null)
            return;
        await _intercomTransport.StartAsync(ct).ConfigureAwait(false);
        await UpdateTransportStatusOnUiAsync().ConfigureAwait(false);
    }

    public async Task<(bool Ok, string Message)> ConnectIntercomTransportAsync(CancellationToken ct = default)
    {
        if (_intercomTransport is null)
            return (false, "Transport не инициализирован.");
        var result = await _intercomTransport.ConnectAsync(ct).ConfigureAwait(false);
        await UpdateTransportStatusOnUiAsync().ConfigureAwait(false);
        return result;
    }

    public async Task DisconnectIntercomTransportAsync(CancellationToken ct = default)
    {
        if (_intercomTransport is null)
            return;
        await _intercomTransport.DisconnectAsync(ct).ConfigureAwait(false);
        await UpdateTransportStatusOnUiAsync().ConfigureAwait(false);
    }

    private void NotifyIntercomTransportAfterPersist(ChatHistoryEvent ev)
    {
        _intercomTransport?.PublishLocalEventFireAndForget(ev);
        _ = UpdateTransportStatusOnUiAsync();
    }

    private async Task TryAutoConnectIntercomTransportAsync()
    {
        if (_intercomTransport is null)
            return;
        await _intercomTransport.TryAutoConnectOnSendAsync().ConfigureAwait(false);
        await UpdateTransportStatusOnUiAsync().ConfigureAwait(false);
    }

    private Task UpdateTransportStatusOnUiAsync()
    {
        if (_intercomTransport is null)
            return Task.CompletedTask;

        var delivery = _intercomTransport.DeliveryStatus;
        var connection = _intercomTransport.ConnectionStatus;
        var text = string.Join(" · ", new[] { connection, delivery }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(text))
            return Task.CompletedTask;

        return UiScheduler.Default.InvokeAsync(() =>
        {
            if (!string.IsNullOrWhiteSpace(connection))
                ClarificationStatusText = text;
        });
    }
}

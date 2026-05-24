#nullable enable

using CascadeIDE.Features.Intercom.Transport;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Координатор team transport Intercom: подключение SSE/HTTP ingest и делегирование в ChatPanel.
/// </summary>
public partial class MainWindowViewModel
{
    private readonly IntercomTransportCoordinator _intercomTransport = new();

    internal IntercomTransportCoordinator IntercomTransport => _intercomTransport;

    public Task<(bool Ok, string Message)> ConnectIntercomTeamTransportAsync(CancellationToken ct = default) =>
        ChatPanel.ConnectIntercomTransportAsync(ct);

    public Task DisconnectIntercomTeamTransportAsync(CancellationToken ct = default) =>
        ChatPanel.DisconnectIntercomTransportAsync(ct);
}

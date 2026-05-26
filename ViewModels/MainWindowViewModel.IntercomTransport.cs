#nullable enable

using CascadeIDE.Features.Intercom.Admin;
using CascadeIDE.Features.Intercom.Transport;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Координатор team transport Intercom: подключение SSE/HTTP ingest и делегирование в ChatPanel.
/// </summary>
public partial class MainWindowViewModel
{
    private readonly IntercomTransportCoordinator _intercomTransport = new();
    private IntercomAdminService? _intercomAdmin;

    internal IntercomTransportCoordinator IntercomTransport => _intercomTransport;

    private IntercomAdminService IntercomAdmin =>
        _intercomAdmin ??= new IntercomAdminService(
            _intercomTransport,
            () => _settings,
            SaveSettingsIfChanged);

    public Task<(bool Ok, string Message)> ConnectIntercomTeamTransportAsync(CancellationToken ct = default) =>
        ChatPanel.ConnectIntercomTransportAsync(ct);

    public Task DisconnectIntercomTeamTransportAsync(CancellationToken ct = default) =>
        ChatPanel.DisconnectIntercomTransportAsync(ct);

    public Task<(bool Ok, string Message)> RefreshIntercomWorkspaceContextAsync(CancellationToken ct = default) =>
        IntercomAdmin.RefreshWorkspaceContextAsync(ct);

    internal Task<CascadeIDE.Features.Chat.ChatSlashIntercomResult> RunIntercomAdminSlashAsync(
        string handlerId,
        string? argsTail,
        CancellationToken ct) =>
        IntercomAdmin.ExecuteAsync(handlerId, argsTail, ct);

    private void RefreshIntercomOnSolutionOpen()
    {
        _intercomTransport.OnWorkspaceChanged();
        _ = RefreshIntercomWorkspaceContextAsync();
    }
}

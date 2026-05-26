using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Привязки <c>[intercom.transport]</c> и Connect/Disconnect (ADR 0144).</summary>
public partial class MainWindowViewModel
{
    public static readonly IReadOnlyList<string> IntercomOAuthProviderOptions = ["github", "oidc"];

    [ObservableProperty] private bool _intercomTransportEnabled;
    [ObservableProperty] private string _intercomTransportBaseUrl = "";
    [ObservableProperty] private string _intercomTransportLocalServerPath = "";
    [ObservableProperty] private string _intercomTransportTeamId = "";
    [ObservableProperty] private string _intercomTransportDefaultTopicId = "";
    [ObservableProperty] private string _intercomTransportOAuthProvider = "github";
    [ObservableProperty] private string _intercomTransportDevTeamToken = "";
    [ObservableProperty] private int _intercomTransportSseReconnectBackoffMs = 1000;
    [ObservableProperty] private bool _intercomTransportAutoConnectOnSend = true;
    [ObservableProperty] private bool _intercomTransportSyncAgentChannelMessages = true;
    [ObservableProperty] private string _intercomTransportConnectionStatus = "";

    [RelayCommand]
    private async Task ConnectIntercomTransportFromSettingsAsync()
    {
        var (ok, message) = await ConnectIntercomTeamTransportAsync().ConfigureAwait(false);
        IntercomTransportConnectionStatus = message;
        if (!ok)
            IntercomTransportConnectionStatus = "Ошибка: " + message;
        else
            IntercomTransportConnectionStatus = IntercomTransport.ConnectionStatus;
    }

    [RelayCommand]
    private async Task DisconnectIntercomTransportFromSettingsAsync()
    {
        await DisconnectIntercomTeamTransportAsync().ConfigureAwait(false);
        IntercomTransportConnectionStatus = IntercomTransport.ConnectionStatus;
    }
}

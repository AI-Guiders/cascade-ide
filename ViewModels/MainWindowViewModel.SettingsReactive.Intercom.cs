namespace CascadeIDE.ViewModels;

/// <summary>Settings reactions: Intercom transport fields.</summary>
public partial class MainWindowViewModel
{
    partial void OnIntercomTransportEnabledChanged(bool value)
    {
        _settings.Intercom.Transport.Enabled = value;
        SaveSettingsIfChanged();
        _ = ChatPanel.StartIntercomTransportAsync();
    }

    partial void OnIntercomTransportBaseUrlChanged(string value)
    {
        _settings.Intercom.Transport.BaseUrl = value?.Trim() ?? "";
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportLocalServerPathChanged(string value)
    {
        _settings.Intercom.Transport.LocalServerPath = value?.Trim() ?? "";
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportTeamIdChanged(string value)
    {
        _settings.Intercom.Transport.TeamId = value?.Trim() ?? "";
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportDefaultTopicIdChanged(string value)
    {
        _settings.Intercom.Transport.DefaultTopicId = value?.Trim() ?? "";
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportOAuthProviderChanged(string value)
    {
        _settings.Intercom.Transport.OAuthProvider = string.IsNullOrWhiteSpace(value) ? "github" : value.Trim();
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportDevTeamTokenChanged(string value)
    {
        _settings.Intercom.Transport.DevTeamToken = value?.Trim() ?? "";
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportSseReconnectBackoffMsChanged(int value)
    {
        var v = Math.Clamp(value, 500, 60_000);
        if (v != value)
        {
            IntercomTransportSseReconnectBackoffMs = v;
            return;
        }

        _settings.Intercom.Transport.SseReconnectBackoffMs = v;
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportAutoConnectOnSendChanged(bool value)
    {
        _settings.Intercom.Transport.AutoConnectOnSend = value;
        SaveSettingsIfChanged();
    }

    partial void OnIntercomTransportSyncAgentChannelMessagesChanged(bool value)
    {
        _settings.Intercom.Transport.SyncAgentChannelMessages = value;
        SaveSettingsIfChanged();
    }
}

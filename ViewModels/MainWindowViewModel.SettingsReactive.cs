namespace CascadeIDE.ViewModels;

/// <summary>Реакции на изменение полей настроек и ключей API: диск, автономный агент, панели.</summary>
public partial class MainWindowViewModel
{
    partial void OnIdeMcpServerEnabledChanged(bool value)
    {
        _settings.IdeMcpServerEnabled = value;
        SaveSettingsIfChanged();
    }

    partial void OnExternalMcpServersJsonChanged(string value)
    {
        _settings.ExternalMcpServersJson = value ?? "[]";

        // External MCP connectivity affects autonomous tool list/calls.
        Autonomous.CancelForHostReconfiguration();
        _mcpClientService = new Services.McpClientService(_settings.ExternalMcpServersJson);
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);
        Autonomous.ReplaceAgentService(_autonomousAgentService);

        SaveSettingsIfChanged();
    }

    partial void OnIsSolutionExplorerVisibleChanged(bool value)
    {
        _settings.SolutionExplorerVisible = value;
        OnPropertyChanged(nameof(IsSolutionPanelHidden));
        SaveSettingsIfChanged();
    }

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        _settings.TerminalVisible = value;
        OnPropertyChanged(nameof(IsTerminalPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
            MfdShellTabIndex = MfdShellTabTerminalIndex;
        else if (MfdShellTabIndex == MfdShellTabTerminalIndex)
            CoerceMfdShellTabToVisible();
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            MfdShellTabIndex = MfdShellTabBuildIndex;
        else if (MfdShellTabIndex == MfdShellTabBuildIndex)
            CoerceMfdShellTabToVisible();
    }

    partial void OnIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.InstrumentationDockVisible = value;
        SaveSettingsIfChanged();
        if (value)
        {
            MfdShellTabIndex = MfdShellTabEventsIndex;
            return;
        }

        if (MfdShellTabIndex is >= MfdShellTabEventsIndex and <= MainWindowViewModel.MfdShellTabDebugStackIndex)
            CoerceMfdShellTabToVisible();
    }

    partial void OnIsChatPanelExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChatPanelHidden));
    }

    partial void OnActiveAiProviderChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _settings.ActiveAiProvider = value;
            SaveSettingsIfChanged();
        }
        ChatPanel.DisposeCursorAcpSession();
        ChatPanel.RefreshSendChatCommandState();
    }

    partial void OnAnthropicApiKeyChanged(string value)
    {
        _aiKeys.AnthropicApiKey = string.IsNullOrEmpty(value) ? null : value;
        SaveAiKeysIfChanged();
    }

    partial void OnOpenAiApiKeyChanged(string value)
    {
        _aiKeys.OpenAiApiKey = string.IsNullOrEmpty(value) ? null : value;
        SaveAiKeysIfChanged();
    }

    partial void OnDeepSeekApiKeyChanged(string value)
    {
        _aiKeys.DeepSeekApiKey = string.IsNullOrEmpty(value) ? null : value;
        SaveAiKeysIfChanged();
    }

    partial void OnIsGitPanelVisibleChanged(bool value)
    {
        _settings.GitPanelVisible = value;
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
        {
            MfdShellTabIndex = MfdShellTabGitIndex;
            _ = GitPanel.RefreshGitPanelAsync();
        }
        else if (MfdShellTabIndex == MfdShellTabGitIndex)
            CoerceMfdShellTabToVisible();
    }

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
    }
}

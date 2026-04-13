using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Реакции на изменение полей настроек и ключей API: диск, автономный агент, панели.</summary>
public partial class MainWindowViewModel
{
    partial void OnIdeMcpServerEnabledChanged(bool value)
    {
        _settings.IdeMcpServerEnabled = value;
        SaveSettingsIfChanged();
    }

    partial void OnMarkdownKrokiEnabledChanged(bool value)
    {
        _settings.MarkdownDiagrams.KrokiEnabled = value;
        SaveSettingsIfChanged();
    }

    partial void OnMarkdownKrokiBaseUrlChanged(string value)
    {
        _settings.MarkdownDiagrams.KrokiBaseUrl = string.IsNullOrWhiteSpace(value) ? "https://kroki.io" : value.Trim();
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
            CurrentSecondaryShellPage = SecondaryShellPage.Terminal;
        else if (CurrentSecondaryShellPage == SecondaryShellPage.Terminal)
            CoerceSecondaryShellPageToAllowed();
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            CurrentSecondaryShellPage = SecondaryShellPage.Build;
        else if (CurrentSecondaryShellPage == SecondaryShellPage.Build)
            CoerceSecondaryShellPageToAllowed();
    }

    partial void OnIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.InstrumentationDockVisible = value;
        SaveSettingsIfChanged();
        if (value)
        {
            CurrentSecondaryShellPage = SecondaryShellPage.Events;
            return;
        }

        if (CurrentSecondaryShellPage is SecondaryShellPage.Events or SecondaryShellPage.Tests or SecondaryShellPage.Hypotheses or SecondaryShellPage.DebugStack)
            CoerceSecondaryShellPageToAllowed();
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
            CurrentSecondaryShellPage = SecondaryShellPage.Git;
            _ = GitPanel.RefreshGitPanelAsync();
        }
        else if (CurrentSecondaryShellPage == SecondaryShellPage.Git)
            CoerceSecondaryShellPageToAllowed();
    }

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
    }
}

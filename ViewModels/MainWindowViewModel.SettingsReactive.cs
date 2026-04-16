using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Реакции на изменение полей настроек и ключей API: диск, автономный агент, панели.</summary>
public partial class MainWindowViewModel
{
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
        _settings.Mcp.ExternalServersJson = value ?? "[]";

        // External MCP connectivity affects autonomous tool list/calls.
        Autonomous.CancelForHostReconfiguration();
        _mcpClientService = new Services.McpClientService(Services.McpExternalServersJsonResolver.ResolveEffectiveJson(_settings));
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);
        Autonomous.ReplaceAgentService(_autonomousAgentService);
        ChatPanel.DisposeCursorAcpSession();

        SaveSettingsIfChanged();
    }

    partial void OnAcpAutoInjectIdeMcpChanged(bool value)
    {
        _settings.Mcp.AcpAutoInjectIdeMcp = value;
        ChatPanel.DisposeCursorAcpSession();
        SaveSettingsIfChanged();
    }

    partial void OnIsPfdRegionExpandedChanged(bool value)
    {
        _settings.WorkspaceUi.PfdRegionExpanded = value;
        OnPropertyChanged(nameof(IsPfdRegionCollapsed));
        SaveSettingsIfChanged();
        if (value)
            ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        _settings.WorkspaceUi.ShowTerminal = value;
        OnPropertyChanged(nameof(IsTerminalPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
            TryNavigateToSecondaryShellPage(SecondaryShellPage.Terminal);
        else if (CurrentSecondaryShellPage == SecondaryShellPage.Terminal)
            CoerceSecondaryShellPageToAllowed();
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            TryNavigateToSecondaryShellPage(SecondaryShellPage.Build);
        else if (CurrentSecondaryShellPage == SecondaryShellPage.Build)
            CoerceSecondaryShellPageToAllowed();
    }

    partial void OnIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.WorkspaceUi.ShowInstrumentation = value;
        SaveSettingsIfChanged();
        if (value)
        {
            TryNavigateToSecondaryShellPage(SecondaryShellPage.Events);
            return;
        }

        if (CurrentSecondaryShellPage is SecondaryShellPage.Events or SecondaryShellPage.Tests or SecondaryShellPage.Hypotheses or SecondaryShellPage.DebugStack)
            CoerceSecondaryShellPageToAllowed();
    }

    partial void OnIsMfdRegionExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMfdRegionCollapsed));
        // Intent «развёрнут/свёрнут регион Mfd» в раскладке (ширина в MainGrid через композитор).
        // Активная страница вторичного контура — отдельно (CurrentSecondaryShellPage); не переключаем её здесь.
    }

    partial void OnChatMcpOnlyChanged(bool value)
    {
        _settings.Ai.ChatMcpOnly = value;
        SaveSettingsIfChanged();
        ChatPanel.RefreshSendChatCommandState();
    }

    partial void OnActiveAiProviderChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _settings.Ai.Provider = value;
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
        _settings.WorkspaceUi.ShowGit = value;
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
        {
            TryNavigateToSecondaryShellPage(SecondaryShellPage.Git);
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

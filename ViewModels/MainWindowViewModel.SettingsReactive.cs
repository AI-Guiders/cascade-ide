using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Реакции на изменение полей настроек и ключей API: диск, автономный агент, панели.</summary>
public partial class MainWindowViewModel
{
    partial void OnMarkdownKrokiEnabledChanged(bool value)
    {
        _settings.Markdown.Diagrams.Kroki = value;
        SaveSettingsIfChanged();
    }

    partial void OnMarkdownKrokiBaseUrlChanged(string value)
    {
        _settings.Markdown.Diagrams.KrokiUrl = string.IsNullOrWhiteSpace(value) ? "https://kroki.io" : value.Trim();
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
        _settings.Workspace.PfdExpanded = value;
        OnPropertyChanged(nameof(IsPfdRegionCollapsed));
        SaveSettingsIfChanged();
        if (value)
            ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        _settings.Workspace.ShowTerminal = value;
        OnPropertyChanged(nameof(IsTerminalPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
            TryNavigateToMfdShellPage(MfdShellPage.Terminal);
        else if (CurrentMfdShellPage == MfdShellPage.Terminal)
            CoerceMfdShellPageToAllowed();
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            TryNavigateToMfdShellPage(MfdShellPage.Build);
        else if (CurrentMfdShellPage == MfdShellPage.Build)
            CoerceMfdShellPageToAllowed();
    }

    partial void OnIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.Workspace.ShowInstrumentation = value;
        SaveSettingsIfChanged();
        if (value)
        {
            TryNavigateToMfdShellPage(MfdShellPage.Events);
            return;
        }

        if (CurrentMfdShellPage is MfdShellPage.Events or MfdShellPage.Tests or MfdShellPage.Hypotheses or MfdShellPage.DebugStack)
            CoerceMfdShellPageToAllowed();
    }

    partial void OnIsMfdRegionExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMfdRegionCollapsed));
        // Intent «развёрнут/свёрнут регион Mfd» в раскладке (ширина в MainGrid через композитор).
        // Активная страница вторичного контура — отдельно (CurrentMfdShellPage); не переключаем её здесь.
    }

    partial void OnShowThinkingInHistoryChanged(bool value)
    {
        _settings.Ai.Chat.ShowThinkingInHistory = value;
        SaveSettingsIfChanged();
    }

    partial void OnAiModeChanged(string value)
    {
        var n = AiSettings.NormalizeMode(value);
        if (!string.Equals(value, n, StringComparison.Ordinal))
        {
            AiMode = n;
            return;
        }

        _settings.Ai.Mode = n;
        OnPropertyChanged(nameof(ActiveAiProvider));
        SaveSettingsIfChanged();
        ChatPanel.DisposeCursorAcpSession();
        ChatPanel.RefreshSendChatCommandState();
    }

    partial void OnCloudActiveProviderChanged(string value)
    {
        var n = AiSettings.NormalizeCloudProvider(value);
        if (!string.Equals(value, n, StringComparison.Ordinal))
        {
            CloudActiveProvider = n;
            return;
        }

        _settings.Ai.Cloud.ActiveProvider = n;
        OnPropertyChanged(nameof(ActiveAiProvider));
        SaveSettingsIfChanged();
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
        _settings.Workspace.ShowGit = value;
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
        {
            TryNavigateToMfdShellPage(MfdShellPage.Git);
            _ = GitPanel.RefreshGitPanelAsync();
        }
        else if (CurrentMfdShellPage == MfdShellPage.Git)
            CoerceMfdShellPageToAllowed();
    }

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
    }

    partial void OnSemanticMapPresentationChanged(string value)
    {
        var normalized = SemanticMapPresentationKind.Normalize(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            SemanticMapPresentation = normalized;
            return;
        }

        _settings.SemanticMap.View = normalized;
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnSemanticMapLevelChanged(string value)
    {
        var normalized = SemanticMapLevelKind.Normalize(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            SemanticMapLevel = normalized;
            return;
        }

        _settings.SemanticMap.Depth = normalized;
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnWorkspaceSplittersLockedChanged(bool value)
    {
        _settings.Workspace.SplittersLocked = value;
        if (_lastSavedSettings is not null)
            SaveSettingsIfChanged();
    }
}

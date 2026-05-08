using CascadeIDE.Features.Shell.Application;
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
        _settings.Markdown.Diagrams.KrokiUrl = ShellSettingsOrchestrator.NormalizeKrokiBaseUrl(value);
        SaveSettingsIfChanged();
    }

    partial void OnExternalMcpServersJsonChanged(string value)
    {
        _settings.Mcp.ExternalServersJson = ShellSettingsOrchestrator.NormalizeExternalMcpServersJson(value);

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
        else if (ShellSettingsOrchestrator.ShouldCoerceCurrentPageWhenHidden(CurrentMfdShellPage, MfdShellPage.Terminal))
            CoerceMfdShellPageToAllowed();
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            TryNavigateToMfdShellPage(MfdShellPage.Build);
        else if (ShellSettingsOrchestrator.ShouldCoerceCurrentPageWhenHidden(CurrentMfdShellPage, MfdShellPage.Build))
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

        if (ShellSettingsOrchestrator.ShouldCoerceWhenInstrumentationHidden(CurrentMfdShellPage))
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
        var n = ShellSettingsOrchestrator.NormalizeAiMode(value);
        if (ShellSettingsOrchestrator.ShouldRewriteWithNormalizedValue(value, n))
        {
            AiMode = n;
            return;
        }

        _settings.Ai.Mode = n;
        OnPropertyChanged(nameof(ActiveAiProvider));
        SaveSettingsIfChanged();
        ChatPanel.DisposeCursorAcpSession();
        ChatPanel.RefreshSendChatCommandState();
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
    }

    partial void OnCloudActiveProviderChanged(string value)
    {
        var n = ShellSettingsOrchestrator.NormalizeCloudProvider(value);
        if (ShellSettingsOrchestrator.ShouldRewriteWithNormalizedValue(value, n))
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
        _aiKeys.AnthropicApiKey = ShellSettingsOrchestrator.NormalizeOptionalSecret(value);
        SaveAiKeysIfChanged();
    }

    partial void OnOpenAiApiKeyChanged(string value)
    {
        _aiKeys.OpenAiApiKey = ShellSettingsOrchestrator.NormalizeOptionalSecret(value);
        SaveAiKeysIfChanged();
    }

    partial void OnDeepSeekApiKeyChanged(string value)
    {
        _aiKeys.DeepSeekApiKey = ShellSettingsOrchestrator.NormalizeOptionalSecret(value);
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
        else if (ShellSettingsOrchestrator.ShouldCoerceCurrentPageWhenHidden(CurrentMfdShellPage, MfdShellPage.Git))
            CoerceMfdShellPageToAllowed();
    }

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
    }

    partial void OnCodeNavigationMapPresentationChanged(string value)
    {
        var normalized = CodeNavigationMapPresentationKind.Normalize(value);
        if (ShellSettingsOrchestrator.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            CodeNavigationMapPresentation = normalized;
            return;
        }

        _settings.CodeNavigationMap.View = normalized;
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnCodeNavigationMapLevelChanged(string value)
    {
        var normalized = CodeNavigationMapLevelKind.Normalize(value);
        if (ShellSettingsOrchestrator.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            CodeNavigationMapLevel = normalized;
            return;
        }

        _settings.CodeNavigationMap.Depth = normalized;
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnWorkspaceSplittersLockedChanged(bool value)
    {
        _settings.Workspace.SplittersLocked = value;
        if (_lastSavedSettings is not null)
            SaveSettingsIfChanged();
    }

    partial void OnHciIntegrationEnabledChanged(bool value)
    {
        _settings.HybridIndex.Enabled = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

    partial void OnHciIndexDirChanged(string value)
    {
        var normalized = ShellSettingsOrchestrator.NormalizeHybridIndexDir(value);
        if (ShellSettingsOrchestrator.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            HciIndexDir = normalized;
            return;
        }

        _settings.HybridIndex.IndexDir = normalized;
        _hybridIndex.SetIndexDirectoryRelative(ResolveHybridIndexDirRelative());
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
        RaiseHybridIndexPresentationProperties();
    }

    partial void OnHciDebounceMsChanged(int value)
    {
        var v = Math.Clamp(value, 0, 60_000);
        if (v != value)
        {
            HciDebounceMs = v;
            return;
        }

        _settings.HybridIndex.DebounceMs = v;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

    partial void OnHciAutoReindexOnSolutionOpenChanged(bool value)
    {
        _settings.HybridIndex.AutoReindexOnSolutionOpen = value;
        SaveSettingsIfChanged();
    }

    partial void OnHciWatchFilesChanged(bool value)
    {
        _settings.HybridIndex.WatchFiles = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

    partial void OnHciScopeModeChanged(string value)
    {
        var n = ShellSettingsOrchestrator.NormalizeHybridIndexScopeMode(value);
        if (ShellSettingsOrchestrator.ShouldRewriteWithNormalizedValue(value, n))
        {
            HciScopeMode = n;
            return;
        }

        _settings.HybridIndex.ScopeMode = n;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
        RaiseHybridIndexPresentationProperties();
    }

    partial void OnHciPauseWhenMcpStdioHostChanged(bool value)
    {
        _settings.HybridIndex.PauseWhenMcpStdioHost = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }
}

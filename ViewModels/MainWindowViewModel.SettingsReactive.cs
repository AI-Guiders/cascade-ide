using CascadeIDE.Features.Settings.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.HybridIndex.Application;
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
        _settings.Markdown.Diagrams.KrokiUrl = ShellSettingsPresentationProjection.NormalizeKrokiBaseUrl(value);
        SaveSettingsIfChanged();
    }

    partial void OnExternalMcpServersJsonChanged(string value) =>
        ShellSettingsReactiveSideEffects.ApplyExternalMcpServersJson(
            ShellSettingsPresentationProjection.NormalizeExternalMcpServersJson(value),
            _settings,
            Autonomous.CancelForHostReconfiguration,
            CreateAutonomousAgentService,
            m => _mcpClientService = m,
            a => _autonomousAgentService = a,
            Autonomous.ReplaceAgentService,
            ChatPanel.DisposeCursorAcpSession,
            SaveSettingsIfChanged);

    partial void OnAcpAutoInjectIdeMcpChanged(bool value)
    {
        _settings.Mcp.AcpAutoInjectIdeMcp = value;
        ChatPanel.DisposeCursorAcpSession();
        SaveSettingsIfChanged();
    }

    partial void OnShowThinkingInHistoryChanged(bool value)
    {
        _settings.Ai.Chat.ShowThinkingInHistory = value;
        SaveSettingsIfChanged();
    }

    partial void OnAiModeChanged(string value)
    {
        var n = ShellSettingsPresentationProjection.NormalizeAiMode(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, n))
        {
            AiMode = n;
            return;
        }

        ShellSettingsReactiveSideEffects.ApplyAiModePersisted(
            n,
            _settings,
            () => OnPropertyChanged(nameof(ActiveAiProvider)),
            SaveSettingsIfChanged,
            ChatPanel.DisposeCursorAcpSession,
            ChatPanel.RefreshSendChatCommandState,
            () => ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false));
    }

    partial void OnCloudActiveProviderChanged(string value)
    {
        var n = ShellSettingsPresentationProjection.NormalizeCloudProvider(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, n))
        {
            CloudActiveProvider = n;
            return;
        }

        ShellSettingsReactiveSideEffects.ApplyCloudActiveProviderPersisted(
            n,
            _settings,
            () => OnPropertyChanged(nameof(ActiveAiProvider)),
            SaveSettingsIfChanged,
            ChatPanel.DisposeCursorAcpSession,
            ChatPanel.RefreshSendChatCommandState);
    }

    partial void OnAnthropicApiKeyChanged(string value)
    {
        _aiKeys.AnthropicApiKey = ShellSettingsPresentationProjection.NormalizeOptionalSecret(value);
        SaveAiKeysIfChanged();
    }

    partial void OnOpenAiApiKeyChanged(string value)
    {
        _aiKeys.OpenAiApiKey = ShellSettingsPresentationProjection.NormalizeOptionalSecret(value);
        SaveAiKeysIfChanged();
    }

    partial void OnDeepSeekApiKeyChanged(string value)
    {
        _aiKeys.DeepSeekApiKey = ShellSettingsPresentationProjection.NormalizeOptionalSecret(value);
        SaveAiKeysIfChanged();
    }

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
        NormalizeChatEnterChordPair();
    }

    partial void OnComposerNewLineKeyChanged(string value)
    {
        _appData.Put("ComposerNewLineKey", value);
        NormalizeChatEnterChordPair();
    }

    partial void OnCodeNavigationMapPresentationChanged(string value)
    {
        var normalized = CodeNavigationMapPresentationKind.Normalize(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
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
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
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
        var normalized = ShellSettingsPresentationProjection.NormalizeHybridIndexDir(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            HciIndexDir = normalized;
            return;
        }

        ShellSettingsReactiveSideEffects.ApplyHybridIndexDirPersisted(
            normalized,
            _settings,
            _hybridIndex,
            () => ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false),
            SaveSettingsIfChanged,
            RaiseHybridIndexPresentationProperties);
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
        var n = ShellSettingsPresentationProjection.NormalizeHybridIndexScopeMode(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, n))
        {
            HciScopeMode = n;
            return;
        }

        ShellSettingsReactiveSideEffects.ApplyHybridIndexScopeModePersisted(
            n,
            _settings,
            () => ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false),
            SaveSettingsIfChanged,
            RaiseHybridIndexPresentationProperties);
    }

    partial void OnHciPauseWhenMcpStdioHostChanged(bool value)
    {
        _settings.HybridIndex.PauseWhenMcpStdioHost = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

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

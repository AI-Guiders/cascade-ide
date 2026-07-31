using CascadeIDE.Features.Settings.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Settings reactions: Markdown/MCP/AI mode/keys/chat chords.
/// HCI → <c>SettingsReactive.HybridIndex</c>; Intercom transport → <c>SettingsReactive.Intercom</c>.</summary>
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
}

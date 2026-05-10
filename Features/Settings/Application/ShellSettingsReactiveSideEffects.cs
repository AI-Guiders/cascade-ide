#nullable enable
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Settings.Application;

/// <summary>
/// Побочные эффекты реакций настроек главного окна: диск, MCP/autonomous, чат, HCI — вне длинных тел partial VM.
/// </summary>
internal static class ShellSettingsReactiveSideEffects
{
    public static void ApplyExternalMcpServersJson(
        string normalizedJson,
        CascadeIdeSettings settings,
        Action cancelAutonomousForHostReconfiguration,
        Func<McpClientService, AutonomousAgentService> createAutonomousAgentService,
        Action<McpClientService> assignMcpClientService,
        Action<AutonomousAgentService> assignAutonomousAgentService,
        Action<AutonomousAgentService> replaceAutonomousAgentOnHost,
        Action disposeCursorAcpSession,
        Action saveSettingsIfChanged)
    {
        settings.Mcp.ExternalServersJson = normalizedJson;
        cancelAutonomousForHostReconfiguration();
        var mcp = new McpClientService(McpExternalServersJsonResolver.ResolveEffectiveJson(settings));
        var autonomous = createAutonomousAgentService(mcp);
        assignMcpClientService(mcp);
        assignAutonomousAgentService(autonomous);
        replaceAutonomousAgentOnHost(autonomous);
        disposeCursorAcpSession();
        saveSettingsIfChanged();
    }

    public static void ApplyAiModePersisted(
        string normalizedMode,
        CascadeIdeSettings settings,
        Action notifyActiveAiProviderChanged,
        Action saveSettingsIfChanged,
        Action disposeCursorAcpSession,
        Action refreshSendChatCommandState,
        Action applyHybridCodebaseIndexOrchestration)
    {
        settings.Ai.Mode = normalizedMode;
        notifyActiveAiProviderChanged();
        saveSettingsIfChanged();
        disposeCursorAcpSession();
        refreshSendChatCommandState();
        applyHybridCodebaseIndexOrchestration();
    }

    public static void ApplyCloudActiveProviderPersisted(
        string normalizedProvider,
        CascadeIdeSettings settings,
        Action notifyActiveAiProviderChanged,
        Action saveSettingsIfChanged,
        Action disposeCursorAcpSession,
        Action refreshSendChatCommandState)
    {
        settings.Ai.Cloud.ActiveProvider = normalizedProvider;
        notifyActiveAiProviderChanged();
        saveSettingsIfChanged();
        disposeCursorAcpSession();
        refreshSendChatCommandState();
    }

    public static void ApplyHybridIndexDirPersisted(
        string normalizedDir,
        CascadeIdeSettings settings,
        HybridIndexOrchestrator hybridIndex,
        Action applyHybridCodebaseIndexOrchestration,
        Action saveSettingsIfChanged,
        Action raiseHybridIndexPresentationProperties)
    {
        settings.HybridIndex.IndexDir = normalizedDir;
        hybridIndex.SetIndexDirectoryRelative(HybridIndexIndexDirectoryRelative.ResolveOrDefault(settings.HybridIndex.IndexDir));
        applyHybridCodebaseIndexOrchestration();
        saveSettingsIfChanged();
        raiseHybridIndexPresentationProperties();
    }

    public static void ApplyHybridIndexScopeModePersisted(
        string normalizedScope,
        CascadeIdeSettings settings,
        Action applyHybridCodebaseIndexOrchestration,
        Action saveSettingsIfChanged,
        Action raiseHybridIndexPresentationProperties)
    {
        settings.HybridIndex.ScopeMode = normalizedScope;
        applyHybridCodebaseIndexOrchestration();
        saveSettingsIfChanged();
        raiseHybridIndexPresentationProperties();
    }
}

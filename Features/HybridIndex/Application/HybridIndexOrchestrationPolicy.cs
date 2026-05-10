#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>
/// Application-level policy for wiring Hybrid Codebase Index orchestration to current settings/runtime.
/// Keeps watcher enablement and debounce decisions out of UI code.
/// </summary>
[ComputingUnit]
public static class HybridIndexOrchestrationPolicy
{
    public static int ResolveDebounceMs(HybridIndexSettings settings)
    {
        var v = settings.DebounceMs;
        if (v < 0) v = 0;
        if (v > 60_000) v = 60_000;
        return v;
    }

    public static bool ShouldEnableWatcher(HybridIndexSettings settings, bool chatMcpOnly) =>
        settings.Enabled
        && settings.WatchFiles
        && !(chatMcpOnly && settings.PauseWhenMcpStdioHost);

    public static void ApplyForCurrentScope(
        HybridIndexOrchestrator orchestrator,
        HybridIndexSettings settings,
        bool chatMcpOnly,
        string workspaceRoot,
        string? solutionPath,
        bool pokeWhenAutoReindex)
    {
        var (hciWs, hciSln) = HybridIndexScopeResolver.ApplyScopeMode(settings.ScopeMode, workspaceRoot, solutionPath);
        if (string.IsNullOrWhiteSpace(hciWs))
            return;

        var enableWatcher = ShouldEnableWatcher(settings, chatMcpOnly);
        orchestrator.SetEnabled(hciWs, hciSln, enabled: enableWatcher, debounceMs: ResolveDebounceMs(settings));
        if (pokeWhenAutoReindex && settings.AutoReindexOnSolutionOpen)
            orchestrator.Poke(hciWs, hciSln);
    }

    /// <summary>
    /// Manual reindex request from UI. Re-applies enablement (watcher may be off) и всегда один полный проход in-proc
    /// со снимком в DataBus; фоновый watcher при необходимости остаётся активен для последующих debounce-прогонов.
    /// </summary>
    public static Task TriggerReindexNowAsync(
        HybridIndexOrchestrator orchestrator,
        HybridIndexSettings settings,
        bool chatMcpOnly,
        string workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken)
    {
        var (hciWs, hciSln) = HybridIndexScopeResolver.ApplyScopeMode(settings.ScopeMode, workspaceRoot, solutionPath);
        if (string.IsNullOrWhiteSpace(hciWs))
            return Task.CompletedTask;

        var enableWatcher = ShouldEnableWatcher(settings, chatMcpOnly);
        orchestrator.SetEnabled(hciWs, hciSln, enabled: enableWatcher, debounceMs: ResolveDebounceMs(settings));

        return orchestrator.RunFullReindexAndPublishStatusAsync(hciWs, hciSln, cancellationToken);
    }
}


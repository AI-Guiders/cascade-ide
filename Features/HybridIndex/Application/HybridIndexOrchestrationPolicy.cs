#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>
/// Application-level policy for wiring Hybrid Codebase Index orchestration to current settings/runtime.
/// Keeps watcher enablement and debounce decisions out of UI code.
/// </summary>
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
    /// Manual reindex request from UI. Re-applies enablement (watcher may be off) and triggers a catch-up.
    /// If watcher is enabled: <see cref="HybridIndexOrchestrator.Poke"/>. Otherwise: full reindex once.
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

        if (enableWatcher)
        {
            orchestrator.Poke(hciWs, hciSln);
            return Task.CompletedTask;
        }

        return orchestrator.RunFullReindexAndPublishStatusAsync(hciWs, hciSln, cancellationToken);
    }
}


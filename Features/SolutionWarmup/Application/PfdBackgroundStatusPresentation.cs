#nullable enable

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Features.SolutionWarmup.Application;

/// <summary>Агрегированная строка статуса фона для полосы на PFD (ADR 0141).</summary>
public static class PfdBackgroundStatusPresentation
{
    public sealed record Snapshot(
        bool Show,
        bool IsCaution,
        string? Text);

    public static Snapshot Compute(
        string? workspaceRoot,
        string? solutionPath,
        SolutionWarmupStateChanged? warmup,
        HybridIndexStateChanged? hci,
        bool hciReindexPending,
        HybridIndexSettings hybridIndex)
    {
        var warmupForScope = MatchesScope(warmup?.WorkspaceRoot, warmup?.SolutionPath, workspaceRoot, solutionPath)
            ? warmup
            : null;
        var hciForScope = MatchesScope(hci?.WorkspaceRoot, hci?.SolutionPath, workspaceRoot, solutionPath)
            ? hci
            : null;

        var warmupRunning = warmupForScope?.Lifecycle == SolutionWarmupLifecycle.Running;
        // Cancelled (scope_changed) — штатная отмена при смене solution, не показываем как ошибку.
        var warmupCaution = warmupForScope?.Lifecycle == SolutionWarmupLifecycle.Partial;
        var hciError = !string.IsNullOrWhiteSpace(hciForScope?.LastError);
        var hciPending = hybridIndex.Enabled
            && hybridIndex.AutoReindexOnSolutionOpen
            && !string.IsNullOrWhiteSpace(workspaceRoot)
            && (hciReindexPending || hciForScope is null);

        if (hciError)
        {
            return new Snapshot(
                true,
                true,
                "Index error — open HCI for details");
        }

        if (warmupCaution)
        {
            var detail = string.IsNullOrWhiteSpace(warmupForScope?.Detail)
                ? "Warm-up incomplete"
                : $"Warm-up: {warmupForScope!.Detail}";
            return new Snapshot(true, true, detail);
        }

        if (warmupRunning && hciPending)
            return new Snapshot(true, false, "Preparing workspace…");

        if (warmupRunning)
            return new Snapshot(true, false, "Warming workspace…");

        if (hciPending)
            return new Snapshot(true, false, "Indexing workspace…");

        return new Snapshot(false, false, null);
    }

    internal static bool MatchesScope(
        string? eventWorkspaceRoot,
        string? eventSolutionPath,
        string? workspaceRoot,
        string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return false;

        if (!pathEquals(eventWorkspaceRoot, workspaceRoot))
            return false;

        if (string.IsNullOrWhiteSpace(solutionPath))
            return string.IsNullOrWhiteSpace(eventSolutionPath);

        return pathEquals(eventSolutionPath, solutionPath);
    }

    private static bool pathEquals(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
            return true;
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        try
        {
            return string.Equals(
                Path.GetFullPath(a.Trim()),
                Path.GetFullPath(b.Trim()),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}

#nullable enable

using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

public enum VerifyRungUiState
{
    Pending,
    Running,
    Pass,
    Fail,
    Cancelled,
    Died,
    Skipped,
}

public sealed record VerifyRungUiEntry(
    string RungId,
    VerifyRungUiState State,
    double DurationSeconds,
    string? Detail);

/// <summary>Shared Verify Epoch formatting for chat + PFD (agent-verify-epoch-view-v1).</summary>
public static class AgentVerifyEpochFormatter
{
    public static readonly string[] OrderedRungs =
    [
        VerifyRung.DiagnoseFiles,
        VerifyRung.CompileProject,
        VerifyRung.BuildAffected,
        VerifyRung.TestScoped,
        VerifyRung.TestFull,
    ];

    public static string? MapTaskKindToRung(string kind) => kind switch
    {
        "roslyn.diagnose" => VerifyRung.DiagnoseFiles,
        "msbuild.compile" => VerifyRung.BuildAffected,
        "dotnet.test" => VerifyRung.TestScoped,
        _ => null,
    };

    public static string FormatCompletedChatTrace(AgentRunCompleted evt)
    {
        var rungLines = FormatRungLinesFromCompleted(evt);
        var status = evt.Green ? "green" : "failed";
        return $"""
            [AEE] verify {evt.RunId[..8]}…
            {rungLines}
              Status: {status} · max rung: {evt.MaxRungReached}
            """;
    }

    public static string FormatRungLinesFromCompleted(AgentRunCompleted evt)
    {
        var entries = BuildEntriesFromTimeSlices(evt.TimeSlices, evt.Green, evt.MaxRungReached);
        return FormatRungLineBlock(entries, includeSkipped: false);
    }

    public static IReadOnlyList<VerifyRungUiEntry> BuildEntriesFromTimeSlices(
        IReadOnlyList<AgentTimeSlice> timeSlices,
        bool green,
        string maxRungReached)
    {
        var byRung = new Dictionary<string, VerifyRungUiEntry>(StringComparer.Ordinal);

        foreach (var slice in timeSlices.Where(s => s.Phase == AgentRunPhaseKind.Environment))
        {
            var rung = TryExtractRungId(slice.Detail);
            if (rung is null)
                continue;

            var state = ResolveStateFromDetail(slice.Detail, green && !IsFailureDetail(slice.Detail));
            byRung[rung] = new VerifyRungUiEntry(rung, state, slice.DurationSeconds, slice.Detail);
        }

        var list = new List<VerifyRungUiEntry>();
        foreach (var rung in OrderedRungs)
        {
            if (byRung.TryGetValue(rung, out var entry))
            {
                list.Add(entry);
                continue;
            }

            if (ShouldMarkSkipped(rung, maxRungReached, green))
                list.Add(new VerifyRungUiEntry(rung, VerifyRungUiState.Skipped, 0, null));
        }

        return list;
    }

    public static string FormatCompactLine(
        bool isActive,
        bool isStale,
        bool displayGreen,
        string? policy,
        string? runId,
        string? activeRungId,
        double activeSeconds,
        string? maxRungReached,
        bool hostDied)
    {
        var policyText = string.IsNullOrWhiteSpace(policy) ? "standard" : policy;

        if (isStale)
            return $"⚠ verify устарел · {policyText}";

        if (hostDied)
            return $"☠ environment died · {policyText}";

        if (isActive)
        {
            var rung = activeRungId ?? VerifyRung.DiagnoseFiles;
            var elapsed = activeSeconds > 0 ? $"{activeSeconds:0.0}s" : "…";
            var shortId = ShortRunId(runId);
            return $"⟳ {rung} {elapsed} · {policyText} · {shortId}";
        }

        if (displayGreen)
            return $"✓ {maxRungReached ?? VerifyRung.BuildAffected} · green ({policyText})";

        if (!string.IsNullOrWhiteSpace(maxRungReached))
            return $"✗ {maxRungReached} · failed ({policyText})";

        return "";
    }

    public static string FormatExpandedBlock(
        string? policy,
        string? runId,
        string? snapshotId,
        bool isStale,
        string? staleReason,
        IReadOnlyList<VerifyRungUiEntry> rungs,
        IReadOnlyList<AgentTimeSlice>? timeSlices,
        bool displayGreen,
        string? maxRungReached)
    {
        var lines = new List<string>
        {
            $"Verify · {policy ?? "standard"} · epoch {ShortRunId(runId)}",
            $"snapshot {ShortSnapshot(snapshotId)}",
        };

        if (isStale)
            lines.Add($"⚠ verify устарел — snapshot изменился ({staleReason ?? "stale"})");

        lines.Add(FormatRungLineBlock(rungs, includeSkipped: true));

        if (timeSlices is { Count: > 0 })
            lines.Add(FormatTimeAccountingFooter(timeSlices, displayGreen, policy, maxRungReached));
        else if (!isStale && !string.IsNullOrWhiteSpace(maxRungReached))
        {
            var status = displayGreen ? "green" : "failed";
            lines.Add($"Status: {status} · max rung: {maxRungReached}");
        }

        return string.Join('\n', lines);
    }

    public static string FormatRungLineBlock(IReadOnlyList<VerifyRungUiEntry> entries, bool includeSkipped)
    {
        if (entries.Count == 0)
            return "  — (no rungs yet)";

        var lines = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.State == VerifyRungUiState.Skipped && !includeSkipped)
                continue;

            var glyph = entry.State switch
            {
                VerifyRungUiState.Pass => "✓",
                VerifyRungUiState.Running => "⟳",
                VerifyRungUiState.Fail => "✗",
                VerifyRungUiState.Cancelled => "⊘",
                VerifyRungUiState.Died => "☠",
                VerifyRungUiState.Skipped => "—",
                _ => "·",
            };

            var duration = entry.State == VerifyRungUiState.Running
                ? "running…"
                : entry.DurationSeconds > 0
                    ? $"{entry.DurationSeconds:0.0}s"
                    : entry.State == VerifyRungUiState.Skipped
                        ? "(not required)"
                        : "—";

            lines.Add($"  {glyph} {entry.RungId,-18} {duration}");
        }

        return string.Join('\n', lines);
    }

    public static string FormatTimeAccountingFooter(
        IReadOnlyList<AgentTimeSlice> slices,
        bool displayGreen,
        string? policy,
        string? maxRungReached)
    {
        double Sum(AgentRunPhaseKind phase) =>
            slices.Where(s => s.Phase == phase).Sum(s => s.DurationSeconds);

        var reasoning = Sum(AgentRunPhaseKind.Reasoning);
        var environment = Sum(AgentRunPhaseKind.Environment);
        var blocked = Sum(AgentRunPhaseKind.Blocked);
        var status = displayGreen ? "green" : "failed";
        return $"Reasoning: {reasoning:0.0}s · Environment: {environment:0.0}s · Blocked: {blocked:0.0}s\nStatus: {status} ({policy ?? "standard"}) · max rung: {maxRungReached ?? "—"}";
    }

    public static bool ShouldDisplayGreen(bool completedGreen, bool isStale, string maxRungReached) =>
        completedGreen && !isStale && !string.IsNullOrWhiteSpace(maxRungReached);

    internal static string? TryExtractRungId(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;

        foreach (var rung in OrderedRungs)
        {
            if (detail.StartsWith(rung, StringComparison.Ordinal)
                || detail.Contains(rung, StringComparison.Ordinal))
                return rung;
        }

        return null;
    }

    internal static bool IsFailureDetail(string? detail) =>
        detail?.Contains("error", StringComparison.OrdinalIgnoreCase) == true
        || detail?.Contains("fail", StringComparison.OrdinalIgnoreCase) == true
        || detail?.Contains("missing", StringComparison.OrdinalIgnoreCase) == true
        || detail?.Contains("cancelled", StringComparison.OrdinalIgnoreCase) == true;

    private static VerifyRungUiState ResolveStateFromDetail(string? detail, bool passHint)
    {
        if (detail?.Contains("ci_parity marker", StringComparison.OrdinalIgnoreCase) == true)
            return VerifyRungUiState.Pass;

        if (detail?.Contains("cancelled", StringComparison.OrdinalIgnoreCase) == true)
            return VerifyRungUiState.Cancelled;

        if (detail?.Contains("died", StringComparison.OrdinalIgnoreCase) == true)
            return VerifyRungUiState.Died;

        if (IsFailureDetail(detail))
            return VerifyRungUiState.Fail;

        return passHint ? VerifyRungUiState.Pass : VerifyRungUiState.Running;
    }

    private static bool ShouldMarkSkipped(string rung, string maxRungReached, bool green)
    {
        var maxIndex = Array.IndexOf(OrderedRungs, maxRungReached);
        var rungIndex = Array.IndexOf(OrderedRungs, rung);
        if (maxIndex < 0 || rungIndex < 0)
            return false;

        return green && rungIndex > maxIndex;
    }

    private static string ShortRunId(string? runId) =>
        string.IsNullOrWhiteSpace(runId) ? "—" : runId.Length <= 6 ? runId : runId[..6];

    private static string ShortSnapshot(string? snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
            return "—";

        return snapshotId.Length <= 12 ? snapshotId : snapshotId[..12] + "…";
    }
}

#nullable enable
using System.Globalization;

namespace CascadeIDE.SoftOrgan;

public sealed record GlassGlanceChip(string Label, string Value, string Tone);

public static class GlassGlanceCards
{
    public static IReadOnlyList<GlassGlanceChip> BuildEvents(GlassEventsGlance.EventsPresenceStatus status)
    {
        var ready = status.LatchLatestCount > 0;
        return
        [
            new("LEVEL", ready ? "READY" : "IDLE", ready ? "ok" : "idle"),
            new("LATCHES", status.LatchLatestCount.ToString(CultureInfo.InvariantCulture), ready ? "ok" : "idle"),
            new("CATALOG", status.Catalog.Count.ToString(CultureInfo.InvariantCulture) + " types", "meta"),
            new("TIMELINE", "Avalonia in-memory", "meta"),
        ];
    }

    public static IReadOnlyList<GlassGlanceChip> BuildWorkspaceHealth(GlassWorkspaceHealthGlance.WorkspaceFsStatus status)
    {
        var level = !status.RootExists ? "MISSING" : status.SlnPath is not null || status.HasGit ? "READY" : "THIN";
        var tone = level switch { "READY" => "ok", "THIN" => "warn", _ => "bad" };
        return
        [
            new("LEVEL", level, tone),
            new("ROOT", status.RootExists ? "ok" : "missing", status.RootExists ? "ok" : "bad"),
            new("GIT", status.HasGit ? "yes" : "no", status.HasGit ? "ok" : "idle"),
            new("SLN", status.SlnPath is { } sln ? Path.GetFileName(sln) : "none", status.SlnPath is not null ? "ok" : "idle"),
            new(".CASCADE-IDE", status.HasCascadeIdeDir ? "yes" : "no", status.HasCascadeIdeDir ? "ok" : "idle"),
        ];
    }

    public static IReadOnlyList<GlassGlanceChip> BuildEnvironment(GlassEnvironmentReadinessGlance.EnvProbeStatus status)
    {
        var level = status.Dotnet.State == "missing"
            ? "MISSING"
            : status.AgentNotes.State == "missing" || status.NetcoreDbg.State == "missing"
                ? "DEGRADED"
                : "READY";
        return
        [
            new("LEVEL", level, level switch { "READY" => "ok", "DEGRADED" => "warn", _ => "bad" }),
            ToChip(status.AgentNotes),
            ToChip(status.NetcoreDbg),
            ToChip(status.Dotnet),
        ];
    }

    public static IReadOnlyList<GlassGlanceChip> BuildHypotheses(GlassHypothesesGlance.HypothesesFsStatus status)
    {
        var level = !status.FileExists ? "MISSING" : status.Total == 0 ? "EMPTY" : "READY";
        return
        [
            new("LEVEL", level, level switch { "READY" => "ok", "EMPTY" => "idle", _ => "bad" }),
            new("TOTAL", status.Total.ToString(CultureInfo.InvariantCulture), status.Total > 0 ? "meta" : "idle"),
            new("OPEN", status.Open.ToString(CultureInfo.InvariantCulture), status.Open > 0 ? "warn" : "idle"),
            new("REJECTED", status.Rejected.ToString(CultureInfo.InvariantCulture), "meta"),
            new("CONFIRMED", status.Confirmed.ToString(CultureInfo.InvariantCulture), status.Confirmed > 0 ? "ok" : "idle"),
        ];
    }

    public readonly record struct FdsShelfStatus(
        bool PlanReady,
        string? PlanPulse,
        bool SharedOn,
        string? SharedFile,
        bool ReportReady,
        string? ReportPulse,
        bool PressureReady,
        string? PressureLine,
        bool WakeReady,
        string? WakeHint,
        bool WorkspaceCdp);

    public static IReadOnlyList<GlassGlanceChip> BuildFds(FdsShelfStatus status)
    {
        var ready = status.PlanReady || status.SharedOn || status.ReportReady || status.PressureReady || status.WakeReady;
        return
        [
            new("LEVEL", ready ? "READY" : "EMPTY", ready ? "ok" : "idle"),
            new("PLAN", status.PlanReady ? Trunc(status.PlanPulse ?? "on", 28) : "miss", status.PlanReady ? "ok" : "idle"),
            new("SHARE", status.SharedOn ? Trunc(status.SharedFile ?? "on", 28) : "off", status.SharedOn ? "ok" : "idle"),
            new("REPORT", status.ReportReady ? Trunc(status.ReportPulse ?? "on", 28) : "miss", status.ReportReady ? "ok" : "idle"),
            new("PRESSURE", status.PressureReady ? Trunc(status.PressureLine ?? "on", 28) : "miss", status.PressureReady ? "warn" : "idle"),
            new("WAKE", status.WakeReady ? Trunc(status.WakeHint ?? "on", 28) : "miss", status.WakeReady ? "warn" : "idle"),
            new(".CDP", status.WorkspaceCdp ? "yes" : "no", status.WorkspaceCdp ? "ok" : "idle"),
        ];
    }

    public static IReadOnlyList<GlassGlanceChip> BuildEditorSitu(GlassEditorSituRibbon.Face face)
    {
        static string V(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : Trunc(s!, 28);
        static string ToneFilled(string? s, string filled) => string.IsNullOrWhiteSpace(s) ? "idle" : filled;

        var blast = face.BlastNames.Count > 0
            ? Trunc(string.Join(" · ", face.BlastNames.Take(2)), 28)
            : V(face.Blast);
        var roleTone = face.Orphan ? "warn" : ToneFilled(face.RoleInGraph, "ok");
        var hops = face.HopLine;
        var hopsTone = string.IsNullOrWhiteSpace(hops) ? "idle" : "ok";
        var lookTone = ToneFilled(face.LookMap, "meta");
        var appliesHasError = !string.IsNullOrWhiteSpace(face.AppliesOnLocus)
            && face.AppliesOnLocus.Contains('E', StringComparison.Ordinal)
            && !face.AppliesOnLocus.Contains("E0", StringComparison.Ordinal);
        var appliesTone = appliesHasError ? "bad" : ToneFilled(face.AppliesOnLocus, "ok");

        return
        [
            new("LEVEL", face.HasAny ? "SITU" : "EMPTY", face.HasAny ? "ok" : "idle"),
            new("WHY", V(face.Why), ToneFilled(face.Why, "ok")),
            new("BLAST", blast, blast == "—" ? "idle" : "ok"),
            new("ROLE", V(face.RoleInGraph), roleTone),
            new("HOPS", V(hops), hopsTone),
            new("LOOK", V(face.LookMap), lookTone),
            new("DIFF", V(face.DiffIntent), ToneFilled(face.DiffIntent, "warn")),
            new("APPLIES", V(face.AppliesOnLocus), appliesTone),
        ];
    }

    public readonly record struct ChatPresenceStatus(string Pf, string Pm);

    public static IReadOnlyList<GlassGlanceChip> BuildChat(ChatPresenceStatus status)
    {
        var pf = string.IsNullOrWhiteSpace(status.Pf) ? "—" : status.Pf.Trim();
        var pm = string.IsNullOrWhiteSpace(status.Pm) ? "—" : status.Pm.Trim();
        var live = IsLivePresence(pf) || IsLivePresence(pm);
        return
        [
            new("LEVEL", live ? "LIVE" : "IDLE", live ? "ok" : "idle"),
            new("@PF", pf, TonePresence(pf)),
            new("@PM", pm, TonePresence(pm)),
            new("SURFACE", "Forward Intercom", "meta"),
        ];
    }

    public static IReadOnlyList<GlassGlanceChip> BuildDomain(GlassDomainBoardGlance.Snapshot snap) =>
        GlassDomainBoardGlance.BuildInstrument(snap);

    static GlassGlanceChip ToChip(GlassEnvironmentReadinessGlance.EnvProbeRow row) =>
        new(row.Name, string.IsNullOrWhiteSpace(row.Detail) ? row.State : $"{row.State} · {row.Detail}", row.State switch
        {
            "ok" => "ok",
            "missing" => "bad",
            "unset" => "idle",
            _ => "warn",
        });

    static string Trunc(string s, int max)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";
    }

    static bool IsLivePresence(string state) =>
        state.Equals("composing", StringComparison.OrdinalIgnoreCase)
        || state.Equals("busy", StringComparison.OrdinalIgnoreCase)
        || state.Equals("stale", StringComparison.OrdinalIgnoreCase);

    static string TonePresence(string state) => state.ToLowerInvariant() switch
    {
        "composing" or "busy" => "ok",
        "stale" => "warn",
        "idle" or "—" or "-" => "idle",
        _ => "meta",
    };
}

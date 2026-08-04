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

    static GlassGlanceChip ToChip(GlassEnvironmentReadinessGlance.EnvProbeRow row) =>
        new(row.Name, string.IsNullOrWhiteSpace(row.Detail) ? row.State : $"{row.State} · {row.Detail}", row.State switch
        {
            "ok" => "ok",
            "missing" => "bad",
            "unset" => "idle",
            _ => "warn",
        });
}

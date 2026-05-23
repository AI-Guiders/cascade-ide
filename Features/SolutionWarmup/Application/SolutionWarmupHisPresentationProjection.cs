using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.SolutionWarmup.Application;

/// <summary>Строки HIS для прогрева solution (ADR 0141 phase C).</summary>
[PresentationProjection("solution-warmup-his")]
public static class SolutionWarmupHisPresentationProjection
{
    public static string StatusShort(SolutionWarmupStateChanged? last) =>
        last?.Lifecycle switch
        {
            null => "—",
            SolutionWarmupLifecycle.Idle => "IDLE",
            SolutionWarmupLifecycle.Running => "WARM",
            SolutionWarmupLifecycle.Ready => "READY",
            SolutionWarmupLifecycle.Partial => "PART",
            SolutionWarmupLifecycle.Cancelled => "CNCL",
            _ => "—",
        };

    public static string StatusLine(SolutionWarmupStateChanged? last)
    {
        if (last is null)
            return "WARM —";

        var detail = string.IsNullOrWhiteSpace(last.Detail) ? "" : $" ({last.Detail})";
        return $"WARM {StatusShort(last)}{detail}";
    }

    public static AnnunciatorLampItem LampItem(SolutionWarmupStateChanged? last)
    {
        if (last is null)
        {
            return new AnnunciatorLampItem(
                Id: "warmup",
                Title: "Warm-up",
                Detail: "No data yet.",
                Level: AnnunciatorLampLevel.Advisory,
                LampShortLabel: "WARM");
        }

        var (level, detail) = last.Lifecycle switch
        {
            SolutionWarmupLifecycle.Ready => (AnnunciatorLampLevel.Ok, "Ready"),
            SolutionWarmupLifecycle.Running => (AnnunciatorLampLevel.Advisory, "Running"),
            SolutionWarmupLifecycle.Partial => (AnnunciatorLampLevel.Caution, last.Detail ?? "Partial"),
            SolutionWarmupLifecycle.Cancelled => (AnnunciatorLampLevel.Advisory, "Cancelled"),
            _ => (AnnunciatorLampLevel.Advisory, "Idle"),
        };

        return new AnnunciatorLampItem(
            Id: "warmup",
            Title: "Warm-up",
            Detail: detail,
            Level: level,
            LampShortLabel: "WARM");
    }
}

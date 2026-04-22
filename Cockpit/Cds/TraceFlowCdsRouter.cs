#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// CDS router for trace-flow channel (ADR 0036 p.2): decides if/where highlights are allowed.
/// </summary>
public interface ITraceFlowCdsRouter : ICdsRouter<TraceFlowCdsRouteInput, TraceFlowCdsDecision>
{
}

public readonly record struct TraceFlowCdsRouteInput(CockpitSurfaceState Cds, string MapLevel);
public readonly record struct TraceFlowCdsDecision(bool Enabled, string ZoneId, string DetailLevel);

public sealed class TraceFlowCdsRouter : ITraceFlowCdsRouter
{
    public TraceFlowCdsDecision Route(TraceFlowCdsRouteInput input)
    {
        var level = CodeNavigationMapLevelKind.Normalize(input.MapLevel);
        var enabled = level == CodeNavigationMapLevelKind.ControlFlow && input.Cds.Zones.PfdVisible;
        return new TraceFlowCdsDecision(
            Enabled: enabled,
            ZoneId: enabled ? "pfd" : "none",
            DetailLevel: enabled ? "normal" : "off");
    }
}

#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// CDS router for trace-flow channel (ADR 0036 p.2): decides if/where highlights are allowed.
/// </summary>
public interface ITraceFlowCdsRouter : ICdsRouter<TraceFlowCdsRouteInput, TraceFlowCdsDecision>
{
}

public readonly record struct TraceFlowCdsRouteInput(CockpitSurfaceState Cds, string SemanticMapLevel);
public readonly record struct TraceFlowCdsDecision(bool Enabled, string ZoneId, string DetailLevel);

public sealed class TraceFlowCdsRouter : ITraceFlowCdsRouter
{
    public TraceFlowCdsDecision Route(TraceFlowCdsRouteInput input)
    {
        var level = SemanticMapLevelKind.Normalize(input.SemanticMapLevel);
        var enabled = level == SemanticMapLevelKind.ControlFlow && input.Cds.Zones.PfdVisible;
        return new TraceFlowCdsDecision(
            Enabled: enabled,
            ZoneId: enabled ? "pfd" : "none",
            DetailLevel: enabled ? "normal" : "off");
    }
}

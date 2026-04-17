#nullable enable
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.Composition.TraceFlow;

/// <summary>
/// Surface compositor for trace-flow channel (ADR 0036 p.3): maps channel payload into view scene highlights.
/// </summary>
public interface ITraceFlowSurfaceCompositor : ISurfaceCompositor<SemanticMapGraphSceneVm, TraceFlowChannelSnapshot, TraceFlowCdsDecision, SemanticMapGraphSceneVm>
{
}

public sealed class TraceFlowSurfaceCompositor : ITraceFlowSurfaceCompositor
{
    public SemanticMapGraphSceneVm Compose(
        SemanticMapGraphSceneVm scene,
        TraceFlowChannelSnapshot snapshot,
        in TraceFlowCdsDecision cdsDecision)
    {
        if (!cdsDecision.Enabled || scene.IsEmpty)
            return scene;

        return new SemanticMapGraphSceneVm
        {
            Nodes = scene.Nodes,
            Edges = scene.Edges,
            HighlightedNodeIds = snapshot.HighlightedNodeIds,
            HighlightedEdgeKeys = snapshot.HighlightedEdgeKeys
        };
    }
}

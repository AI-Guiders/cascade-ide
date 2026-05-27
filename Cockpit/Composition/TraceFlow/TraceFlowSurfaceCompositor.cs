#nullable enable
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.Composition.TraceFlow;

/// <summary>
/// Surface compositor for trace-flow channel (ADR 0036 p.3): maps channel payload into view scene highlights.
/// </summary>
public interface ITraceFlowSurfaceCompositor : ISurfaceCompositor<CodeNavigationMapGraphSceneVm, TraceFlowChannelSnapshot, TraceFlowCdsDecision, CodeNavigationMapGraphSceneVm>
{
}

public sealed class TraceFlowSurfaceCompositor : ITraceFlowSurfaceCompositor
{
    public CodeNavigationMapGraphSceneVm Compose(
        CodeNavigationMapGraphSceneVm scene,
        TraceFlowChannelSnapshot snapshot,
        in TraceFlowCdsDecision cdsDecision)
    {
        if (!cdsDecision.Enabled || scene.IsEmpty)
            return scene;

        return new CodeNavigationMapGraphSceneVm
        {
            Nodes = scene.Nodes,
            Edges = scene.Edges,
            Legend = scene.Legend,
            UseLegendColumn = scene.UseLegendColumn,
            ShowLegendConditionKey = scene.ShowLegendConditionKey,
            ShowLegendReturnKey = scene.ShowLegendReturnKey,
            ShowLegendExceptionFlowKey = scene.ShowLegendExceptionFlowKey,
            ShowLegendEdgeStyleKey = scene.ShowLegendEdgeStyleKey,
            LegendColumnLeft = scene.LegendColumnLeft,
            LegendPlacement = scene.LegendPlacement,
            LegendBlockTopY = scene.LegendBlockTopY,
            Presentation = scene.Presentation,
            HighlightedNodeIds = snapshot.HighlightedNodeIds,
            HighlightedEdgeKeys = snapshot.HighlightedEdgeKeys,
            SideLabelFontSizePx = scene.SideLabelFontSizePx,
            ShowNodeLegendGlyphs = scene.ShowNodeLegendGlyphs,
            RelatedFilesLayout = scene.RelatedFilesLayout,
            ControlFlowMainAxis = scene.ControlFlowMainAxis,
            LayoutViewportWidth = scene.LayoutViewportWidth,
            LayoutViewportHeight = scene.LayoutViewportHeight
        };
    }
}

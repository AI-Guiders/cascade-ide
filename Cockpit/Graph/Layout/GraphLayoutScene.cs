#nullable enable
using Avalonia;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>
/// Уложенная сцена graph-backed surface: геометрия узлов/рёбер для Render-слоя (ADR 0055, 0067).
/// Не зависит от ViewModels; источник — layout stage после <see cref="GraphDocument"/>.
/// </summary>
public sealed class GraphLayoutScene
{
    public required IReadOnlyList<GraphLayoutNode> Nodes { get; init; }
    public required IReadOnlyList<GraphLayoutEdge> Edges { get; init; }

    public GraphLayoutPresentation Presentation { get; init; } = GraphLayoutPresentation.CodeControlFlow;
    public IReadOnlyList<GraphLegendEntry> Legend { get; init; } = [];
    public bool UseLegendColumn { get; init; }
    public bool ShowLegendConditionKey { get; init; }
    public bool ShowLegendReturnKey { get; init; }
    public bool ShowLegendExceptionFlowKey { get; init; }
    public bool ShowLegendEdgeStyleKey { get; init; }
    public double LegendColumnLeft { get; init; } = double.PositiveInfinity;
    public GraphLegendBlockPlacement LegendPlacement { get; init; } = GraphLegendBlockPlacement.BesideGraph;
    public double LegendBlockTopY { get; init; }
    public IReadOnlySet<string> HighlightedNodeIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> HighlightedEdgeKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public double? SideLabelFontSizePx { get; init; }

    /// <summary>Номера шагов на узлах без колонки легенды (code anchor UX).</summary>
    public bool ShowNodeLegendGlyphs { get; init; }

    /// <summary>Укладка related-files (<c>radial</c> | <c>top_down</c> | <c>bottom_up</c>); только для <see cref="GraphLayoutPresentation.WorkspaceRelatedFiles"/>.</summary>
    public string RelatedFilesLayout { get; init; } = Models.CodeNavigationMapRelatedGraphLayoutKind.Radial;

    /// <summary>CFG: главная ось потока (автовыбор в <see cref="ControlFlowGraphLayoutEngine"/>).</summary>
    public GraphControlFlowMainAxis ControlFlowMainAxis { get; init; } = GraphControlFlowMainAxis.Vertical;

    public bool IsEmpty => Nodes.Count == 0;

    public GraphLayoutScene WithPresentation(GraphLayoutPresentation presentation)
    {
        if (Presentation == presentation)
            return this;
        return new GraphLayoutScene
        {
            Nodes = Nodes,
            Edges = Edges,
            Presentation = presentation,
            Legend = Legend,
            UseLegendColumn = UseLegendColumn,
            ShowLegendConditionKey = ShowLegendConditionKey,
            ShowLegendReturnKey = ShowLegendReturnKey,
            ShowLegendExceptionFlowKey = ShowLegendExceptionFlowKey,
            ShowLegendEdgeStyleKey = ShowLegendEdgeStyleKey,
            LegendColumnLeft = LegendColumnLeft,
            LegendPlacement = LegendPlacement,
            LegendBlockTopY = LegendBlockTopY,
            HighlightedNodeIds = HighlightedNodeIds,
            HighlightedEdgeKeys = HighlightedEdgeKeys,
            SideLabelFontSizePx = SideLabelFontSizePx,
            ShowNodeLegendGlyphs = ShowNodeLegendGlyphs,
            RelatedFilesLayout = RelatedFilesLayout,
            ControlFlowMainAxis = ControlFlowMainAxis
        };
    }
}

public sealed class GraphLegendEntry
{
    public int Index { get; init; }
    public required string Text { get; init; }
}

public sealed class GraphLayoutNode
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string FullPath { get; init; }
    public required string Label { get; init; }
    public required Point Center { get; init; }
    public required double Radius { get; init; }
    public required bool IsAnchor { get; init; }
    public GraphNodeShape Shape { get; init; } = GraphNodeShape.Circle;
    public int? LegendIndex { get; init; }
    public string? LegendLine { get; init; }
    public int? LineStart { get; init; }
    public int? LineEnd { get; init; }
    /// <summary>Тот же id, что у <see cref="GraphNode.LoopGroupId"/>; овал на миникарте группирует по этому значению.</summary>
    public int? LoopGroupId { get; init; }
}

public sealed class GraphLayoutEdge
{
    public required string FromNodeId { get; init; }
    public required string ToNodeId { get; init; }
    public required Point From { get; init; }
    public required Point To { get; init; }
    public required double ToRadius { get; init; }
    public string? Kind { get; init; }
    public string? RelationKind { get; init; }
    public string? EdgeProvenance { get; init; }
    public string? BranchLabel { get; init; }

    public string Key => $"{FromNodeId}->{ToNodeId}";
}

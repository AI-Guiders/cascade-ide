#nullable enable
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Мост <see cref="GraphLayoutScene"/> ↔ binding VM (ADR 0055 layout/render split).</summary>
public static class CodeNavigationMapGraphSceneProjection
{
    public static CodeNavigationMapGraphSceneVm ToViewModel(
        GraphLayoutScene scene,
        double layoutViewportWidth = 0,
        double layoutViewportHeight = 0,
        CodeNavigationMapSettings? mapSettings = null,
        string? solutionPath = null)
    {
        var branchLabels = CodeNavigationMapConditionBranchLabels.Resolve(mapSettings, solutionPath);
        return new CodeNavigationMapGraphSceneVm
        {
            Nodes = scene.Nodes.Select(MapNode).ToList(),
            Edges = scene.Edges.Select(e => MapEdge(e, branchLabels)).ToList(),
            Presentation = MapPresentation(scene.Presentation),
            Legend = scene.Legend.Select(e => new CodeNavigationMapLegendEntry { Index = e.Index, Text = e.Text }).ToList(),
            UseLegendColumn = scene.UseLegendColumn,
            ShowLegendConditionKey = scene.ShowLegendConditionKey,
            ShowLegendReturnKey = scene.ShowLegendReturnKey,
            ShowLegendExceptionFlowKey = scene.ShowLegendExceptionFlowKey,
            ShowLegendEdgeStyleKey = scene.ShowLegendEdgeStyleKey,
            LegendColumnLeft = scene.LegendColumnLeft,
            LegendPlacement = MapLegendPlacement(scene.LegendPlacement),
            LegendBlockTopY = scene.LegendBlockTopY,
            HighlightedNodeIds = scene.HighlightedNodeIds,
            HighlightedEdgeKeys = scene.HighlightedEdgeKeys,
            SideLabelFontSizePx = scene.SideLabelFontSizePx,
            ShowNodeLegendGlyphs = scene.ShowNodeLegendGlyphs,
            RelatedFilesLayout = scene.RelatedFilesLayout,
            ControlFlowMainAxis = scene.ControlFlowMainAxis,
            LayoutViewportWidth = layoutViewportWidth,
            LayoutViewportHeight = layoutViewportHeight
        };
    }

    public static GraphLayoutScene FromViewModel(CodeNavigationMapGraphSceneVm scene) =>
        new()
        {
            Nodes = scene.Nodes.Select(ToLayoutNode).ToList(),
            Edges = scene.Edges.Select(ToLayoutEdge).ToList(),
            Presentation = MapPresentationToLayout(scene.Presentation),
            Legend = scene.Legend.Select(e => new GraphLegendEntry { Index = e.Index, Text = e.Text }).ToList(),
            UseLegendColumn = scene.UseLegendColumn,
            ShowLegendConditionKey = scene.ShowLegendConditionKey,
            ShowLegendReturnKey = scene.ShowLegendReturnKey,
            ShowLegendExceptionFlowKey = scene.ShowLegendExceptionFlowKey,
            ShowLegendEdgeStyleKey = scene.ShowLegendEdgeStyleKey,
            LegendColumnLeft = scene.LegendColumnLeft,
            LegendPlacement = MapLegendPlacementToLayout(scene.LegendPlacement),
            LegendBlockTopY = scene.LegendBlockTopY,
            HighlightedNodeIds = scene.HighlightedNodeIds,
            HighlightedEdgeKeys = scene.HighlightedEdgeKeys,
            SideLabelFontSizePx = scene.SideLabelFontSizePx,
            ShowNodeLegendGlyphs = scene.ShowNodeLegendGlyphs,
            RelatedFilesLayout = scene.RelatedFilesLayout,
            ControlFlowMainAxis = scene.ControlFlowMainAxis
        };

    public static CodeNavigationMapGraphNodeLayout MapNode(GraphLayoutNode n) =>
        new()
        {
            Id = n.Id,
            Kind = n.Kind,
            FullPath = n.FullPath,
            Label = n.Label,
            Center = n.Center,
            Radius = n.Radius,
            IsAnchor = n.IsAnchor,
            Shape = MapNodeShape(n.Shape),
            LegendIndex = n.LegendIndex,
            LegendLine = n.LegendLine,
            LineStart = n.LineStart,
            LineEnd = n.LineEnd,
            LoopGroupId = n.LoopGroupId
        };

    public static CodeNavigationMapGraphEdgeLayout MapEdge(
        GraphLayoutEdge e,
        CodeNavigationMapConditionBranchLabels.Pair? branchLabels = null) =>
        new()
        {
            FromNodeId = e.FromNodeId,
            ToNodeId = e.ToNodeId,
            From = e.From,
            To = e.To,
            ToRadius = e.ToRadius,
            Kind = e.Kind,
            RelatedKind = e.RelationKind,
            BranchLabel = branchLabels is null
                ? e.BranchLabel
                : CodeNavigationMapConditionBranchLabels.ResolveDisplayLabel(e.EdgeProvenance, branchLabels)
                    ?? e.BranchLabel
        };

    public static GraphLayoutNode ToLayoutNode(CodeNavigationMapGraphNodeLayout n) =>
        new()
        {
            Id = n.Id,
            Kind = n.Kind,
            FullPath = n.FullPath,
            Label = n.Label,
            Center = n.Center,
            Radius = n.Radius,
            IsAnchor = n.IsAnchor,
            Shape = MapNodeShapeToLayout(n.Shape),
            LegendIndex = n.LegendIndex,
            LegendLine = n.LegendLine,
            LineStart = n.LineStart,
            LineEnd = n.LineEnd,
            LoopGroupId = n.LoopGroupId
        };

    public static GraphLayoutEdge ToLayoutEdge(CodeNavigationMapGraphEdgeLayout e) =>
        new()
        {
            FromNodeId = e.FromNodeId,
            ToNodeId = e.ToNodeId,
            From = e.From,
            To = e.To,
            ToRadius = e.ToRadius,
            Kind = e.Kind,
            RelationKind = e.RelatedKind,
            BranchLabel = e.BranchLabel
        };

    private static CodeNavigationMapGraphPresentationKind MapPresentation(GraphLayoutPresentation presentation) =>
        presentation == GraphLayoutPresentation.WorkspaceRelatedFiles
            ? CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles
            : CodeNavigationMapGraphPresentationKind.CodeControlFlow;

    private static GraphLayoutPresentation MapPresentationToLayout(CodeNavigationMapGraphPresentationKind kind) =>
        kind == CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles
            ? GraphLayoutPresentation.WorkspaceRelatedFiles
            : GraphLayoutPresentation.CodeControlFlow;

    private static CodeNavigationMapLegendBlockPlacement MapLegendPlacement(GraphLegendBlockPlacement placement) =>
        placement == GraphLegendBlockPlacement.BelowGraph
            ? CodeNavigationMapLegendBlockPlacement.BelowGraph
            : CodeNavigationMapLegendBlockPlacement.BesideGraph;

    private static GraphLegendBlockPlacement MapLegendPlacementToLayout(CodeNavigationMapLegendBlockPlacement placement) =>
        placement == CodeNavigationMapLegendBlockPlacement.BelowGraph
            ? GraphLegendBlockPlacement.BelowGraph
            : GraphLegendBlockPlacement.BesideGraph;

    private static CodeNavigationMapNodeShape MapNodeShape(GraphNodeShape shape) =>
        shape == GraphNodeShape.Condition ? CodeNavigationMapNodeShape.Condition : CodeNavigationMapNodeShape.Circle;

    private static GraphNodeShape MapNodeShapeToLayout(CodeNavigationMapNodeShape shape) =>
        shape == CodeNavigationMapNodeShape.Condition ? GraphNodeShape.Condition : GraphNodeShape.Circle;
}

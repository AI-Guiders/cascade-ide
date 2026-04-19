using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Цвета и перья сцены Semantic Map (control flow / звезда). Единый источник для <see cref="SemanticMapSceneDrawing"/> (ADR 0064, 0055, 0067).
/// </summary>
public sealed class SemanticMapVisualTheme
{
    private SemanticMapVisualTheme(
        Color anchorFill,
        Color conditionFill,
        Color exitFill,
        Color callFill,
        Color sideLabel,
        Color baseEdge,
        Color conditionalEdge,
        Color multiBranchEdge,
        Color loopEdge,
        Color highlightedEdge,
        Color highlightedLoopEdge,
        Color highlightedNode,
        Color nodeStroke)
    {
        AnchorFill = new SolidColorBrush(anchorFill);
        ConditionFill = new SolidColorBrush(conditionFill);
        ExitFill = new SolidColorBrush(exitFill);
        CallFill = new SolidColorBrush(callFill);
        SideLabelBrush = new SolidColorBrush(sideLabel);
        BaseEdgePen = new(new SolidColorBrush(baseEdge), 1);
        ConditionalEdgePen = new(new SolidColorBrush(conditionalEdge), 1.2)
        {
            DashStyle = new DashStyle([3, 2], 0)
        };
        MultiBranchEdgePen = new(new SolidColorBrush(multiBranchEdge), 1)
        {
            DashStyle = new DashStyle([2, 2], 0)
        };
        LoopEdgePen = new(new SolidColorBrush(loopEdge), 1.8);
        HighlightedEdgePen = new(new SolidColorBrush(highlightedEdge), 1.8);
        HighlightedLoopEdgePen = new(new SolidColorBrush(highlightedLoopEdge), 2.2);
        HighlightedNodePen = new(new SolidColorBrush(highlightedNode), 1.2);
        NodeStrokePen = new(new SolidColorBrush(nodeStroke), 1);
    }

    /// <summary>Тема по умолчанию: CFG / control flow.</summary>
    public static SemanticMapVisualTheme Default { get; } = new(
        CockpitPrimitivesPalette.SemanticMap.AnchorFill,
        CockpitPrimitivesPalette.SemanticMap.ConditionFill,
        CockpitPrimitivesPalette.SemanticMap.ExitFill,
        CockpitPrimitivesPalette.SemanticMap.CallFill,
        CockpitPrimitivesPalette.SemanticMap.SideLabel,
        CockpitPrimitivesPalette.SemanticMap.BaseEdge,
        CockpitPrimitivesPalette.SemanticMap.ConditionalEdge,
        CockpitPrimitivesPalette.SemanticMap.MultiBranchEdge,
        CockpitPrimitivesPalette.SemanticMap.LoopEdge,
        CockpitPrimitivesPalette.SemanticMap.HighlightedEdge,
        CockpitPrimitivesPalette.SemanticMap.HighlightedLoopEdge,
        CockpitPrimitivesPalette.SemanticMap.HighlightedNode,
        CockpitPrimitivesPalette.SemanticMap.NodeStroke);

    private static readonly SemanticMapVisualTheme WorkspaceRelated = new(
        CockpitPrimitivesPalette.SemanticMapWorkspace.AnchorFill,
        CockpitPrimitivesPalette.SemanticMapWorkspace.ConditionFill,
        CockpitPrimitivesPalette.SemanticMapWorkspace.ExitFill,
        CockpitPrimitivesPalette.SemanticMapWorkspace.PeerFill,
        CockpitPrimitivesPalette.SemanticMapWorkspace.SideLabel,
        CockpitPrimitivesPalette.SemanticMapWorkspace.BaseEdge,
        CockpitPrimitivesPalette.SemanticMapWorkspace.ConditionalEdge,
        CockpitPrimitivesPalette.SemanticMapWorkspace.MultiBranchEdge,
        CockpitPrimitivesPalette.SemanticMapWorkspace.LoopEdge,
        CockpitPrimitivesPalette.SemanticMapWorkspace.HighlightedEdge,
        CockpitPrimitivesPalette.SemanticMapWorkspace.HighlightedLoopEdge,
        CockpitPrimitivesPalette.SemanticMapWorkspace.HighlightedNode,
        CockpitPrimitivesPalette.SemanticMapWorkspace.NodeStroke);

    public static SemanticMapVisualTheme ForPresentation(SemanticMapGraphPresentationKind presentation) =>
        presentation == SemanticMapGraphPresentationKind.WorkspaceRelatedFiles
            ? WorkspaceRelated
            : Default;

    public IBrush AnchorFill { get; }
    public IBrush ConditionFill { get; }
    public IBrush ExitFill { get; }
    public IBrush CallFill { get; }
    public IBrush GlyphBrush { get; } = Brushes.White;
    public IBrush SideLabelBrush { get; }
    public Pen BaseEdgePen { get; }
    public Pen ConditionalEdgePen { get; }
    public Pen MultiBranchEdgePen { get; }
    public Pen LoopEdgePen { get; }
    public Pen HighlightedEdgePen { get; }
    public Pen HighlightedLoopEdgePen { get; }
    public Pen HighlightedNodePen { get; }
    public Pen NodeStrokePen { get; }
    public Typeface GlyphTypeface { get; } = new("Segoe UI", FontStyle.Normal, FontWeight.SemiBold);
    public Typeface SideLabelTypeface { get; } = new("Segoe UI", FontStyle.Normal, FontWeight.Medium);
}

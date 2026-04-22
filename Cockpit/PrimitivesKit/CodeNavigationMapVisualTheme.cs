using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Цвета и перья сцены мини-карты (control flow / звезда; в UI — «Карта кода»). Единый источник для <see cref="CodeNavigationMapSceneDrawing"/> (ADR 0064, 0055, 0067).
/// </summary>
public sealed class CodeNavigationMapVisualTheme
{
    private CodeNavigationMapVisualTheme(
        Color anchorFill,
        Color conditionFill,
        Color exitFill,
        Color callFill,
        Color handlerFill,
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
        HandlerFill = new SolidColorBrush(handlerFill);
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
    public static CodeNavigationMapVisualTheme Default { get; } = new(
        CockpitPrimitivesPalette.CodeNavigationMap.AnchorFill,
        CockpitPrimitivesPalette.CodeNavigationMap.ConditionFill,
        CockpitPrimitivesPalette.CodeNavigationMap.ExitFill,
        CockpitPrimitivesPalette.CodeNavigationMap.CallFill,
        CockpitPrimitivesPalette.CodeNavigationMap.HandlerFill,
        CockpitPrimitivesPalette.CodeNavigationMap.SideLabel,
        CockpitPrimitivesPalette.CodeNavigationMap.BaseEdge,
        CockpitPrimitivesPalette.CodeNavigationMap.ConditionalEdge,
        CockpitPrimitivesPalette.CodeNavigationMap.MultiBranchEdge,
        CockpitPrimitivesPalette.CodeNavigationMap.LoopEdge,
        CockpitPrimitivesPalette.CodeNavigationMap.HighlightedEdge,
        CockpitPrimitivesPalette.CodeNavigationMap.HighlightedLoopEdge,
        CockpitPrimitivesPalette.CodeNavigationMap.HighlightedNode,
        CockpitPrimitivesPalette.CodeNavigationMap.NodeStroke);

    private static readonly CodeNavigationMapVisualTheme WorkspaceRelated = new(
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.AnchorFill,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.ConditionFill,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.ExitFill,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.PeerFill,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.HandlerFill,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.SideLabel,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.BaseEdge,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.ConditionalEdge,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.MultiBranchEdge,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.LoopEdge,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.HighlightedEdge,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.HighlightedLoopEdge,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.HighlightedNode,
        CockpitPrimitivesPalette.CodeNavigationMapWorkspace.NodeStroke);

    public static CodeNavigationMapVisualTheme ForPresentation(CodeNavigationMapGraphPresentationKind presentation) =>
        presentation == CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles
            ? WorkspaceRelated
            : Default;

    public IBrush AnchorFill { get; }
    public IBrush ConditionFill { get; }
    public IBrush ExitFill { get; }
    public IBrush CallFill { get; }
    public IBrush HandlerFill { get; }
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

using Avalonia.Media;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Цвета и перья сцены Semantic Map (control flow / звезда). Единый источник для <see cref="SemanticMapSceneDrawing"/> (ADR 0064, 0055).
/// </summary>
public sealed class SemanticMapVisualTheme
{
    public static SemanticMapVisualTheme Default { get; } = new();

    public IBrush AnchorFill { get; } = new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.AnchorFill);
    public IBrush ConditionFill { get; } = new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.ConditionFill);
    public IBrush ExitFill { get; } = new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.ExitFill);
    public IBrush CallFill { get; } = new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.CallFill);
    public IBrush GlyphBrush { get; } = Brushes.White;
    public IBrush SideLabelBrush { get; } = new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.SideLabel);
    public Pen BaseEdgePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.BaseEdge), 1);
    public Pen ConditionalEdgePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.ConditionalEdge), 1.2)
    {
        DashStyle = new DashStyle([3, 2], 0)
    };
    public Pen MultiBranchEdgePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.MultiBranchEdge), 1)
    {
        DashStyle = new DashStyle([2, 2], 0)
    };
    public Pen LoopEdgePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.LoopEdge), 1.8);
    public Pen HighlightedEdgePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.HighlightedEdge), 1.8);
    public Pen HighlightedLoopEdgePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.HighlightedLoopEdge), 2.2);
    public Pen HighlightedNodePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.HighlightedNode), 1.2);
    public Pen NodeStrokePen { get; } = new(new SolidColorBrush(CockpitPrimitivesPalette.SemanticMap.NodeStroke), 1);
    public Typeface GlyphTypeface { get; } = new("Segoe UI", FontStyle.Normal, FontWeight.SemiBold);
    public Typeface SideLabelTypeface { get; } = new("Segoe UI", FontStyle.Normal, FontWeight.Medium);
}

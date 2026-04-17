using Avalonia.Media;

namespace CascadeIDE.Views;

internal sealed class SemanticMapVisualTheme
{
    public static SemanticMapVisualTheme Default { get; } = new();

    public IBrush AnchorFill { get; } = new SolidColorBrush(Color.Parse("#7CC9FF"));
    public IBrush ConditionFill { get; } = new SolidColorBrush(Color.FromArgb(240, 255, 210, 120));
    public IBrush ExitFill { get; } = new SolidColorBrush(Color.FromArgb(230, 165, 175, 190));
    public IBrush CallFill { get; } = new SolidColorBrush(Color.Parse("#9B8CFF"));
    public IBrush GlyphBrush { get; } = Brushes.White;
    public IBrush SideLabelBrush { get; } = new SolidColorBrush(Color.FromArgb(220, 225, 235, 255));
    public Pen BaseEdgePen { get; } = new(new SolidColorBrush(Color.FromArgb(180, 140, 140, 160)), 1);
    public Pen ConditionalEdgePen { get; } = new(new SolidColorBrush(Color.FromArgb(220, 255, 210, 120)), 1.2)
    {
        DashStyle = new DashStyle([3, 2], 0)
    };
    public Pen MultiBranchEdgePen { get; } = new(new SolidColorBrush(Color.FromArgb(200, 110, 195, 255)), 1)
    {
        DashStyle = new DashStyle([2, 2], 0)
    };
    public Pen LoopEdgePen { get; } = new(new SolidColorBrush(Color.FromArgb(235, 120, 230, 255)), 1.8);
    public Pen HighlightedEdgePen { get; } = new(new SolidColorBrush(Color.FromArgb(245, 255, 255, 190)), 1.8);
    public Pen HighlightedLoopEdgePen { get; } = new(new SolidColorBrush(Color.FromArgb(250, 255, 255, 200)), 2.2);
    public Pen HighlightedNodePen { get; } = new(new SolidColorBrush(Color.FromArgb(230, 255, 255, 200)), 1.2);
    public Pen NodeStrokePen { get; } = new(new SolidColorBrush(Color.Parse("#22000000")), 1);
    public Typeface GlyphTypeface { get; } = new("Segoe UI", FontStyle.Normal, FontWeight.SemiBold);
    public Typeface SideLabelTypeface { get; } = new("Segoe UI", FontStyle.Normal, FontWeight.Medium);
}

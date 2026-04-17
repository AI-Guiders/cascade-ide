using System.Windows.Input;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>Мини-карта Semantic Map: рёбра и узлы (звезда); клик по узлу открывает файл; подписи строк — в списке (режим list/both) (ADR 0039).</summary>
public sealed class SemanticMapMiniMapControl : Control
{
    private const string ConditionStepKind = "condition_step";
    private const string ExitStepKind = "exit_step";
    private static readonly SemanticMapVisualTheme VisualTheme = SemanticMapVisualTheme.Default;

    public static readonly StyledProperty<SemanticMapGraphSceneVm?> SceneProperty =
        AvaloniaProperty.Register<SemanticMapMiniMapControl, SemanticMapGraphSceneVm?>(nameof(Scene));

    public static readonly StyledProperty<ICommand?> OpenFileCommandProperty =
        AvaloniaProperty.Register<SemanticMapMiniMapControl, ICommand?>(nameof(OpenFileCommand));

    static SemanticMapMiniMapControl()
    {
        AffectsRender<SemanticMapMiniMapControl>(SceneProperty);
        HeightProperty.OverrideDefaultValue<SemanticMapMiniMapControl>(120);
        MinWidthProperty.OverrideDefaultValue<SemanticMapMiniMapControl>(200);
    }

    public SemanticMapGraphSceneVm? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public ICommand? OpenFileCommand
    {
        get => GetValue(OpenFileCommandProperty);
        set => SetValue(OpenFileCommandProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var scene = Scene;
        var cmd = OpenFileCommand;
        if (scene is null || cmd is null)
            return;
        var p = e.GetPosition(this);
        foreach (var n in scene.Nodes)
        {
            var dx = p.X - n.Center.X;
            var dy = p.Y - n.Center.Y;
            if (Math.Sqrt(dx * dx + dy * dy) > n.Radius + 6)
                continue;
            var path = n.FullPath;
            if (string.IsNullOrWhiteSpace(path))
                continue;
            if (!cmd.CanExecute(path))
                continue;
            try
            {
                cmd.Execute(path);
                e.Handled = true;
            }
            catch
            {
                // RelayCommand/ICommand: не роняем UI при кривом пути или сбое дока
            }

            return;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scene = Scene;
        if (scene is null || scene.IsEmpty)
            return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        DrawEdges(context, scene);
        DrawNodes(context, scene);
    }

    private static void DrawEdges(DrawingContext context, SemanticMapGraphSceneVm scene)
    {
        var previousWasLoop = false;
        foreach (var edge in scene.Edges)
        {
            var isHighlighted = scene.HighlightedEdgeKeys.Contains(edge.Key);
            var isLoop = IsLoopEdge(edge.Kind);
            if (isLoop && !previousWasLoop)
            {
                var basePen = isHighlighted ? VisualTheme.HighlightedEdgePen : VisualTheme.BaseEdgePen;
                var loopPen = isHighlighted ? VisualTheme.HighlightedLoopEdgePen : VisualTheme.LoopEdgePen;
                DrawLoopEdge(context, edge, basePen, loopPen);
                previousWasLoop = true;
                continue;
            }

            var edgeStyle = ResolveEdgePen(edge.Kind, isHighlighted);
            context.DrawLine(edgeStyle, edge.From, edge.To);
            previousWasLoop = isLoop;
        }
    }

    private static void DrawNodes(DrawingContext context, SemanticMapGraphSceneVm scene)
    {
        foreach (var n in scene.Nodes)
        {
            var highlighted = scene.HighlightedNodeIds.Contains(n.Id);
            context.DrawEllipse(ResolveNodeFill(n), VisualTheme.NodeStrokePen, n.Center, n.Radius, n.Radius);
            if (highlighted)
                context.DrawEllipse(null, VisualTheme.HighlightedNodePen, n.Center, n.Radius + 3, n.Radius + 3);

            var glyph = BuildNodeGlyph(n);
            var glyphText = new FormattedText(
                glyph,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                VisualTheme.GlyphTypeface,
                Math.Max(8, n.Radius - 2),
                VisualTheme.GlyphBrush);
            var glyphOrigin = new Point(
                n.Center.X - glyphText.Width / 2,
                n.Center.Y - glyphText.Height / 2);
            context.DrawText(glyphText, glyphOrigin);

            var fullLabel = BuildNodeFullLabel(n);
            if (!string.IsNullOrWhiteSpace(fullLabel))
            {
                var labelText = new FormattedText(
                    fullLabel,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    VisualTheme.SideLabelTypeface,
                    11,
                    VisualTheme.SideLabelBrush);
                var labelOrigin = new Point(
                    n.Center.X + n.Radius + 6,
                    n.Center.Y - labelText.Height / 2);
                context.DrawText(labelText, labelOrigin);
            }
        }
    }

    private static Pen ResolveEdgePen(string? kind, bool highlighted)
    {
        if (highlighted)
            return VisualTheme.HighlightedEdgePen;
        if (IsMultiBranchEdge(kind))
            return VisualTheme.MultiBranchEdgePen;
        if (IsConditionalEdge(kind))
            return VisualTheme.ConditionalEdgePen;
        return VisualTheme.BaseEdgePen;
    }

    private static IBrush ResolveNodeFill(SemanticMapGraphNodeLayout node)
    {
        if (node.IsAnchor)
            return VisualTheme.AnchorFill;
        if (IsConditionNode(node))
            return VisualTheme.ConditionFill;
        if (IsExitNode(node))
            return VisualTheme.ExitFill;
        return VisualTheme.CallFill;
    }

    private static bool IsLoopEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("loop", StringComparison.OrdinalIgnoreCase);

    private static bool IsMultiBranchEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("multibranch", StringComparison.OrdinalIgnoreCase);

    private static void DrawLoopEdge(DrawingContext context, SemanticMapGraphEdgeLayout edge, Pen linePen, Pen loopPen)
    {
        var vx = edge.To.X - edge.From.X;
        var vy = edge.To.Y - edge.From.Y;
        var len = Math.Sqrt(vx * vx + vy * vy);
        if (len < 1)
        {
            context.DrawLine(loopPen, edge.From, edge.To);
            return;
        }

        var nx = vx / len;
        var ny = vy / len;
        var entry = new Point(
            edge.To.X - nx * (edge.ToRadius + 10),
            edge.To.Y - ny * (edge.ToRadius + 10));
        context.DrawLine(linePen, edge.From, entry);

        var loopRadius = edge.ToRadius + 11;
        context.DrawEllipse(null, loopPen, edge.To, loopRadius, loopRadius);
    }

    private static string BuildNodeGlyph(SemanticMapGraphNodeLayout node)
    {
        if (node.IsAnchor)
            return "A";
        if (IsConditionNode(node))
            return "?";
        if (IsExitNode(node))
            return "↗";
        return "•";
    }

    private static string? BuildNodeFullLabel(SemanticMapGraphNodeLayout node)
    {
        if (node.IsAnchor || IsConditionNode(node) || IsExitNode(node))
            return null;
        var label = node.Label?.Trim();
        if (string.IsNullOrWhiteSpace(label))
            return null;
        return label;
    }

    private static bool IsExitNode(SemanticMapGraphNodeLayout node) =>
        string.Equals(node.Kind, ExitStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionNode(SemanticMapGraphNodeLayout node) =>
        string.Equals(node.Kind, ConditionStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionalEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("conditional", StringComparison.OrdinalIgnoreCase);
}

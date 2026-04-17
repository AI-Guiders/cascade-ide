using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>Мини-карта Semantic Map: рёбра и узлы (звезда); клик по узлу открывает файл; подписи строк — в списке (режим list/both) (ADR 0039).</summary>
public sealed class SemanticMapMiniMapControl : Control
{
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

        var edgePen = new Pen(new SolidColorBrush(Color.FromArgb(180, 140, 140, 160)), 1);
        var multibranchPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 110, 195, 255)), 1)
        {
            DashStyle = new DashStyle([2, 2], 0)
        };
        var loopPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 120, 230, 255)), 1.4);
        foreach (var edge in scene.Edges)
        {
            if (IsLoopEdge(edge.Kind))
            {
                DrawLoopEdge(context, edge, edgePen, loopPen);
                continue;
            }

            context.DrawLine(IsMultiBranchEdge(edge.Kind) ? multibranchPen : edgePen, edge.From, edge.To);
        }

        foreach (var n in scene.Nodes)
        {
            var fill = n.IsAnchor
                ? new SolidColorBrush(Color.Parse("#7CC9FF"))
                : new SolidColorBrush(Color.Parse("#9B8CFF"));
            var stroke = new SolidColorBrush(Color.Parse("#22000000"));
            context.DrawEllipse(fill, new Pen(stroke, 1), n.Center, n.Radius, n.Radius);
        }
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

        var loopRadius = edge.ToRadius + 8;
        context.DrawEllipse(null, loopPen, edge.To, loopRadius, loopRadius);
    }
}

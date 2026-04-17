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
        foreach (var edge in scene.Edges)
            context.DrawLine(edgePen, edge.From, edge.To);

        foreach (var n in scene.Nodes)
        {
            var fill = n.IsAnchor
                ? new SolidColorBrush(Color.Parse("#7CC9FF"))
                : new SolidColorBrush(Color.Parse("#9B8CFF"));
            var stroke = new SolidColorBrush(Color.Parse("#22000000"));
            context.DrawEllipse(fill, new Pen(stroke, 1), n.Center, n.Radius, n.Radius);
        }
    }
}

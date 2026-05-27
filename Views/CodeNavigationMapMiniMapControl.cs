using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.ViewModels;
using CascadeIDE.Views.SkiaKit.Graph;

namespace CascadeIDE.Views;

/// <summary>Мини-карта навигации по коду: рёбра и узлы (звезда); клик по узлу открывает файл; подписи строк — в списке (режим list/both) (ADR 0039).</summary>
/// <remarks>
/// Порядок отрисовки базовой сцены (ADR 0055 §4): рёбра → узлы (фигуры и глифы) → легенда. Подсветки TraceFlow приходят в <see cref="CodeNavigationMapGraphSceneVm"/> и рисуются вместе с рёбрами/узлами по флагам highlight.
/// Отрисовка — <see cref="SkiaGraphSceneDrawing"/> (SkiaKit, ADR 0117).
/// </remarks>
public sealed class CodeNavigationMapMiniMapControl : Control
{
    public static readonly StyledProperty<CodeNavigationMapGraphSceneVm?> SceneProperty =
        AvaloniaProperty.Register<CodeNavigationMapMiniMapControl, CodeNavigationMapGraphSceneVm?>(nameof(Scene));

    public static readonly StyledProperty<ICommand?> OpenFileCommandProperty =
        AvaloniaProperty.Register<CodeNavigationMapMiniMapControl, ICommand?>(nameof(OpenFileCommand));

    static CodeNavigationMapMiniMapControl()
    {
        AffectsRender<CodeNavigationMapMiniMapControl>(SceneProperty);
        HeightProperty.OverrideDefaultValue<CodeNavigationMapMiniMapControl>(SkiaGraphViewportMetrics.DefaultHeightFile);
        MinWidthProperty.OverrideDefaultValue<CodeNavigationMapMiniMapControl>(200);
    }

    public CodeNavigationMapGraphSceneVm? Scene
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
        var layout = GraphLayoutSceneMapper.FromViewModel(scene);
        var p = SkiaGraphSceneHitTesting.MapControlPointToLayout(
            e.GetPosition(this),
            Bounds.Width,
            Bounds.Height,
            scene.LayoutViewportWidth,
            scene.LayoutViewportHeight);
        var hit = SkiaGraphSceneHitTesting.FindNodeAt(layout, p);
        if (hit is null || string.IsNullOrWhiteSpace(hit.FullPath))
            return;

        var vmNode = scene.Nodes.FirstOrDefault(n => string.Equals(n.Id, hit.Id, StringComparison.OrdinalIgnoreCase));
        if (vmNode is null)
            return;

        var payload = new CodeNavigationMapNodeNavigatePayload(
            vmNode.FullPath,
            vmNode.LineStart,
            vmNode.LineEnd,
            vmNode.LegendLine,
            vmNode.Kind);
        if (!cmd.CanExecute(payload))
            return;
        try
        {
            cmd.Execute(payload);
            e.Handled = true;
        }
        catch
        {
            // RelayCommand/ICommand: не роняем UI при кривом пути или сбое дока
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

        var layout = GraphLayoutSceneMapper.FromViewModel(scene);
        var theme = SkiaGraphVisualTheme.ForPresentation(layout.Presentation);
        SkiaGraphSceneDrawing.DrawScene(context, layout, theme, w, h);
    }
}

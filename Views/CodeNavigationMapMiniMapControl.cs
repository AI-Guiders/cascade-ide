using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>Мини-карта навигации по коду: рёбра и узлы (звезда); клик по узлу открывает файл; подписи строк — в списке (режим list/both) (ADR 0039).</summary>
/// <remarks>
/// Порядок отрисовки базовой сцены (ADR 0055 §4): рёбра → узлы (фигуры и глифы) → легенда. Подсветки TraceFlow приходят в <see cref="CodeNavigationMapGraphSceneVm"/> и рисуются вместе с рёбрами/узлами по флагам highlight.
/// Отрисовка — <see cref="CodeNavigationMapSceneDrawing"/> (ADR 0064).
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
        HeightProperty.OverrideDefaultValue<CodeNavigationMapMiniMapControl>(CodeNavigationMapGraphPrimitives.DefaultViewportHeightFile);
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
        var p = e.GetPosition(this);
        foreach (var n in scene.Nodes)
        {
            if (!CodeNavigationMapSceneDrawing.HitTestNode(n, p))
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

        var theme = CodeNavigationMapVisualTheme.ForPresentation(scene.Presentation);
        CodeNavigationMapSceneDrawing.DrawScene(context, scene, theme, w, h);
    }
}

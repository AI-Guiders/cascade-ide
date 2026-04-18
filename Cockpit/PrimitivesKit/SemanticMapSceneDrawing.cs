using Avalonia;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Точка входа: отрисовка сцены Semantic Map. Геометрия разнесена по partial-файлам (рёбра / узлы / легенда).
/// </summary>
public static partial class SemanticMapSceneDrawing
{
    private const string ConditionStepKind = "condition_step";
    private const string ExitStepKind = "exit_step";

    public static void DrawScene(
        DrawingContext context,
        SemanticMapGraphSceneVm scene,
        SemanticMapVisualTheme theme,
        double width,
        double height)
    {
        if (scene.IsEmpty || width <= 0 || height <= 0)
            return;

        DrawEdges(context, scene, theme);
        DrawNodes(context, scene, theme);
        DrawLegend(context, scene, theme, width, height);
    }

    /// <summary>Hit-test узла для pointer routing (узел условия — ромб, манхэттенское расстояние до ромба).</summary>
    public static bool HitTestNode(SemanticMapGraphNodeLayout n, Point p, double tolerance = 6)
    {
        if (n.Shape == SemanticMapNodeShape.Condition)
            return HitConditionBranchOutline(n.Center, n.Radius, p, tolerance);
        var dx = p.X - n.Center.X;
        var dy = p.Y - n.Center.Y;
        return Math.Sqrt(dx * dx + dy * dy) <= n.Radius + tolerance;
    }

    private static bool HitConditionBranchOutline(Point center, double r, Point p, double tolerance)
    {
        var dx = Math.Abs(p.X - center.X);
        var dy = Math.Abs(p.Y - center.Y);
        return dx + dy <= r + tolerance;
    }
}

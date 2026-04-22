using Avalonia;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Точка входа: отрисовка сцены мини-карты навигации по коду (в UI — «Карта намерений»). Геометрия — partial (рёбра / узлы / легенда).
/// </summary>
public static partial class CodeNavigationMapSceneDrawing
{
    private const string ConditionStepKind = "condition_step";
    private const string ExitStepKind = "exit_step";

    /// <summary>Стрелка «выход из зоны» (NE): не зависит от глифа ↗ в шрифте.</summary>
    internal static void DrawNorthEastExitArrow(DrawingContext context, IBrush stroke, Point tip, double size, double thickness = 1.35)
    {
        if (size < 2)
            return;

        var dirX = 0.70710678118654757;
        var dirY = -0.70710678118654757;
        var basePt = new Point(tip.X - dirX * size, tip.Y - dirY * size);
        var pen = new Pen(stroke, thickness) { LineCap = PenLineCap.Round };
        context.DrawLine(pen, basePt, tip);
        var wing = size * 0.38;
        var px = -dirY;
        var py = dirX;
        var w1 = new Point(
            tip.X - dirX * wing + px * wing * 0.35,
            tip.Y - dirY * wing + py * wing * 0.35);
        context.DrawLine(pen, w1, tip);
        var w2 = new Point(
            tip.X - dirX * wing - px * wing * 0.35,
            tip.Y - dirY * wing - py * wing * 0.35);
        context.DrawLine(pen, w2, tip);
    }

    /// <summary>
    /// Та же NE-стрелка, но середина ствола (от <see cref="DrawNorthEastExitArrow"/>) совпадает с
    /// <paramref name="shaftCenter"/> — визуально «по центру кружка», без смещения 0.12·R.
    /// </summary>
    internal static void DrawNorthEastExitArrowShaftCentered(
        DrawingContext context, IBrush stroke, Point shaftCenter, double size, double thickness = 1.35)
    {
        if (size < 2)
            return;
        const double h = 0.70710678118654757;
        var tip = new Point(shaftCenter.X + h * size * 0.5, shaftCenter.Y - h * size * 0.5);
        DrawNorthEastExitArrow(context, stroke, tip, size, thickness);
    }

    public static void DrawScene(
        DrawingContext context,
        CodeNavigationMapGraphSceneVm scene,
        CodeNavigationMapVisualTheme theme,
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
    public static bool HitTestNode(CodeNavigationMapGraphNodeLayout n, Point p, double tolerance = 6)
    {
        if (n.Shape == CodeNavigationMapNodeShape.Condition)
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

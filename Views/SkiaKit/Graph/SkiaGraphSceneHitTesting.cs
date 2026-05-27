#nullable enable
using Avalonia;
using CascadeIDE.Cockpit.Graph.Layout;

namespace CascadeIDE.Views.SkiaKit.Graph;

/// <summary>Pointer hit-test для сцены графа (порядок отрисовки, масштаб viewport).</summary>
public static class SkiaGraphSceneHitTesting
{
    /// <summary>Узел под точкой <paramref name="pointInLayoutSpace"/> или null.</summary>
    public static GraphLayoutNode? FindNodeAt(GraphLayoutScene scene, Point pointInLayoutSpace)
    {
        GraphLayoutNode? best = null;
        var bestDistance = double.MaxValue;

        for (var i = scene.Nodes.Count - 1; i >= 0; i--)
        {
            var n = scene.Nodes[i];
            if (!SkiaGraphSceneDrawing.HitTestNode(n, pointInLayoutSpace))
                continue;

            var dx = pointInLayoutSpace.X - n.Center.X;
            var dy = pointInLayoutSpace.Y - n.Center.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = n;
        }

        return best;
    }

    /// <summary>Координаты клика в control → логические координаты укладки.</summary>
    public static Point MapControlPointToLayout(
        Point controlPoint,
        double controlWidth,
        double controlHeight,
        double layoutViewportWidth,
        double layoutViewportHeight)
    {
        if (layoutViewportWidth > 0 && layoutViewportHeight > 0
            && controlWidth > 0 && controlHeight > 0
            && (Math.Abs(controlWidth - layoutViewportWidth) > 0.5
                || Math.Abs(controlHeight - layoutViewportHeight) > 0.5))
        {
            return new Point(
                controlPoint.X * layoutViewportWidth / controlWidth,
                controlPoint.Y * layoutViewportHeight / controlHeight);
        }

        return controlPoint;
    }
}

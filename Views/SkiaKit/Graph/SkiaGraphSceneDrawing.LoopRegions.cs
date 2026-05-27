#nullable enable
using Avalonia;
using Avalonia.Media;
using CascadeIDE.Cockpit.Graph.Layout;

namespace CascadeIDE.Views.SkiaKit.Graph;

public static partial class SkiaGraphSceneDrawing
{
    /// <summary>Овал вокруг узлов одного <see cref="GraphLayoutNode.LoopGroupId"/> (цикл while/for/foreach/do).</summary>
    private static void DrawLoopGroupOutlines(DrawingContext context, GraphLayoutScene scene, SkiaGraphVisualTheme theme)
    {
        if (scene.Presentation != GraphLayoutPresentation.CodeControlFlow || scene.Nodes.Count == 0)
            return;

        if (theme.LoopEdgePen.Brush is not SolidColorBrush scb)
            return;

        var loopColor = scb.Color;
        var stroke = new SolidColorBrush(
            Color.FromArgb((byte)Math.Clamp((int)(loopColor.A * 0.40), 22, 96), loopColor.R, loopColor.G, loopColor.B));
        var dashPen = new Pen(stroke, 1.2) { DashStyle = new DashStyle([7, 4], 0) };

        foreach (var g in scene.Nodes
                     .Where(n => n.LoopGroupId > 0)
                     .GroupBy(n => n.LoopGroupId!.Value))
        {
            var nodes = g.ToList();
            if (nodes.Count == 0)
                continue;

            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            foreach (var n in nodes)
            {
                var rEff = n.Shape == GraphNodeShape.Condition ? n.Radius * 1.08 : n.Radius;
                minX = Math.Min(minX, n.Center.X - rEff);
                maxX = Math.Max(maxX, n.Center.X + rEff);
                minY = Math.Min(minY, n.Center.Y - rEff);
                maxY = Math.Max(maxY, n.Center.Y + rEff);
            }

            const double pad = 14;
            var cx = (minX + maxX) * 0.5;
            var cy = (minY + maxY) * 0.5;
            var rx = (maxX - minX) * 0.5 + pad;
            var ry = (maxY - minY) * 0.5 + pad;
            if (rx < 6 || ry < 6)
                continue;

            context.DrawEllipse(null, dashPen, new Point(cx, cy), rx, ry);
        }
    }
}

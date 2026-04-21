using Avalonia;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

public static partial class SemanticMapSceneDrawing
{
    private static void DrawEdges(DrawingContext context, SemanticMapGraphSceneVm scene, SemanticMapVisualTheme theme)
    {
        var previousWasLoop = false;
        foreach (var edge in scene.Edges)
        {
            var isHighlighted = scene.HighlightedEdgeKeys.Contains(edge.Key);
            var isLoop = IsLoopEdge(edge.Kind);
            if (isLoop && !previousWasLoop)
            {
                var basePen = isHighlighted ? theme.HighlightedEdgePen : theme.BaseEdgePen;
                var loopPen = isHighlighted ? theme.HighlightedLoopEdgePen : theme.LoopEdgePen;
                DrawLoopEdge(context, scene, theme, edge, basePen, loopPen);
                previousWasLoop = true;
                continue;
            }

            var edgeStyle = ResolveEdgePen(theme, edge.Kind, isHighlighted);
            var fromR = GetNodeRadius(scene, edge.FromNodeId, fallback: 12);
            DrawCubicEdge(context, edgeStyle, edge.From, fromR, edge.To, edge.ToRadius);
            previousWasLoop = isLoop;
        }
    }

    private static Pen ResolveEdgePen(SemanticMapVisualTheme theme, string? kind, bool highlighted)
    {
        if (highlighted)
            return theme.HighlightedEdgePen;
        if (IsMultiBranchEdge(kind))
            return theme.MultiBranchEdgePen;
        if (IsConditionalEdge(kind))
            return theme.ConditionalEdgePen;
        return theme.BaseEdgePen;
    }

    private static bool IsLoopEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("loop", StringComparison.OrdinalIgnoreCase);

    private static bool IsMultiBranchEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("multibranch", StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionalEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("conditional", StringComparison.OrdinalIgnoreCase);

    private static void DrawLoopEdge(
        DrawingContext context,
        SemanticMapGraphSceneVm scene,
        SemanticMapVisualTheme theme,
        SemanticMapGraphEdgeLayout edge,
        Pen linePen,
        Pen loopPen)
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
        var fromR = GetNodeRadius(scene, edge.FromNodeId, fallback: 12);
        DrawCubicEdge(context, linePen, edge.From, fromR, entry, toRadius: null);

        var loopRadius = edge.ToRadius + 11;
        context.DrawEllipse(null, loopPen, edge.To, loopRadius, loopRadius);
    }

    private static double GetNodeRadius(SemanticMapGraphSceneVm scene, string nodeId, double fallback)
    {
        foreach (var n in scene.Nodes)
        {
            if (string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase))
                return n.Radius;
        }

        return fallback;
    }

    private static void DrawCubicEdge(
        DrawingContext context,
        Pen pen,
        Point fromCenter,
        double fromRadius,
        Point to,
        double? toRadius)
    {
        var dx = to.X - fromCenter.X;
        var dy = to.Y - fromCenter.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6)
            return;
        var ux = dx / len;
        var uy = dy / len;
        var start = new Point(fromCenter.X + ux * fromRadius, fromCenter.Y + uy * fromRadius);
        Point end = toRadius is { } tr
            ? new Point(to.X - ux * tr, to.Y - uy * tr)
            : to;

        var ex = end.X - start.X;
        var ey = end.Y - start.Y;
        var elen = Math.Sqrt(ex * ex + ey * ey);
        if (elen < 1e-6)
            return;
        if (elen < 6)
        {
            context.DrawLine(pen, start, end);
            DrawArrowHeadAtTip(context, pen, end, ex / elen, ey / elen);
            return;
        }

        // Для почти вертикальных/последовательных шагов (1->2->3->4) рисуем прямую,
        // иначе «дуга из-за нехватки места» визуально читается как лишний обход.
        var horizontalDrift = Math.Abs(ex);
        var isNearStraightFlow = horizontalDrift <= Math.Max(8, elen * 0.08);
        if (isNearStraightFlow)
        {
            context.DrawLine(pen, start, end);
            DrawArrowHeadAtTip(context, pen, end, ex / elen, ey / elen);
            return;
        }

        var bendByDistance = Math.Min(42, elen * 0.2);
        var bendByHorizontalRoom = Math.Max(0, horizontalDrift * 0.45);
        var bend = Math.Min(bendByDistance, bendByHorizontalRoom);
        var px = -ey / elen;
        var py = ex / elen;
        var c1 = new Point(start.X + ex / 3 + px * bend, start.Y + ey / 3 + py * bend);
        var c2 = new Point(end.X - ex / 3 + px * bend, end.Y - ey / 3 + py * bend);

        var geometry = new StreamGeometry();
        using (var ig = geometry.Open())
        {
            ig.BeginFigure(start, false);
            ig.CubicBezierTo(c1, c2, end);
        }

        context.DrawGeometry(null, pen, geometry);
        var tdx = end.X - c2.X;
        var tdy = end.Y - c2.Y;
        var tlen = Math.Sqrt(tdx * tdx + tdy * tdy);
        if (tlen < 1e-6)
        {
            tdx = end.X - start.X;
            tdy = end.Y - start.Y;
            tlen = Math.Sqrt(tdx * tdx + tdy * tdy);
        }

        if (tlen >= 1e-6)
            DrawArrowHeadAtTip(context, pen, end, tdx / tlen, tdy / tlen);
    }

    private static void DrawArrowHeadAtTip(DrawingContext context, Pen pen, Point tip, double dirX, double dirY)
    {
        var brush = pen.Brush ?? Brushes.White;
        var thickness = pen.Thickness;
        if (thickness <= 0)
            thickness = 1;
        var arrowLen = 6 + Math.Min(5, thickness * 1.8);
        var halfW = arrowLen * 0.45;
        var bx = tip.X - dirX * arrowLen;
        var by = tip.Y - dirY * arrowLen;
        var px = -dirY;
        var py = dirX;
        var p0 = new Point(bx + px * halfW, by + py * halfW);
        var p1 = new Point(bx - px * halfW, by - py * halfW);
        var geo = new StreamGeometry();
        using (var ig = geo.Open())
        {
            ig.BeginFigure(tip, true);
            ig.LineTo(p0);
            ig.LineTo(p1);
            ig.EndFigure(true);
        }

        context.DrawGeometry(brush, null, geo);
    }
}

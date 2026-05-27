using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using CascadeIDE.Cockpit.Graph.Layout;

namespace CascadeIDE.Views.SkiaKit.Graph;

public static partial class SkiaGraphSceneDrawing
{
    private static void DrawEdges(DrawingContext context, GraphLayoutScene scene, SkiaGraphVisualTheme theme)
    {
        var previousWasLoop = false;
        foreach (var edge in scene.Edges)
        {
            var isHighlighted = scene.HighlightedEdgeKeys.Contains(edge.Key);
            var isLoop = IsLoopEdge(edge.Kind);
            if (isLoop && !previousWasLoop)
            {
                var baseLoopPen = isHighlighted ? theme.HighlightedLoopEdgePen : theme.LoopEdgePen;
                var loopPen = ScaleLoopPen(baseLoopPen, ResolveLoopWeightScale(scene, edge), isHighlighted);
                DrawLoopEdge(context, scene, edge, loopPen);
                previousWasLoop = true;
                continue;
            }

            var edgeStyle = ResolveEdgePen(theme, edge.Kind, isHighlighted);
            var fromR = GetNodeRadius(scene, edge.FromNodeId, fallback: 12);
            DrawCubicEdge(context, edgeStyle, edge.From, fromR, edge.To, edge.ToRadius);
            if (!string.IsNullOrEmpty(edge.BranchLabel))
                DrawBranchLabel(context, theme, scene, edge, fromR, edge.BranchLabel);
            previousWasLoop = isLoop;
        }
    }

    private static Pen ResolveEdgePen(SkiaGraphVisualTheme theme, string? kind, bool highlighted)
    {
        if (highlighted)
            return theme.HighlightedEdgePen;
        if (IsMultiBranchEdge(kind))
            return theme.MultiBranchEdgePen;
        if (IsConditionalEdge(kind))
            return theme.ConditionalEdgePen;
        if (IsExceptionFlowEdge(kind))
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

    private static bool IsExceptionFlowEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("exception", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// ADR 0053: «петля на ребре» — один штрих пунктирной кубики с усиленным боковым выносом к цели, без второго декоративного эллипса по узлу.
    /// </summary>
    private static void DrawLoopEdge(
        DrawingContext context,
        GraphLayoutScene scene,
        GraphLayoutEdge edge,
        Pen loopPen)
    {
        var fromR = GetNodeRadius(scene, edge.FromNodeId, fallback: 12);
        DrawCubicEdge(
            context,
            loopPen,
            edge.From,
            fromR,
            edge.To,
            edge.ToRadius,
            lateralBendMultiplier: LoopEdgeOrbitBendMultiplier(scene, edge));
    }

    /// <summary>Чуть сильнее «орбита» при вертикальном главном потоке и при большей группе цикла (ADR 0053).</summary>
    private static double LoopEdgeOrbitBendMultiplier(GraphLayoutScene scene, GraphLayoutEdge edge)
    {
        var baseMul = scene.ControlFlowMainAxis == GraphControlFlowMainAxis.Horizontal ? 2.05 : 2.2;
        var dx = edge.To.X - edge.From.X;
        var dy = edge.To.Y - edge.From.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len > 140)
            baseMul *= 1.05;
        var members = ResolveLoopGroupMemberCount(scene, edge);
        if (members > 1)
            baseMul *= 1.0 + Math.Min(0.18, (members - 1) * 0.04);
        return baseMul;
    }

    /// <summary>Толщина/контраст петли по числу узлов в <see cref="GraphLayoutNode.LoopGroupId"/>.</summary>
    private static double ResolveLoopWeightScale(GraphLayoutScene scene, GraphLayoutEdge edge)
    {
        var members = ResolveLoopGroupMemberCount(scene, edge);
        return members switch
        {
            <= 1 => 1.0,
            2 => 1.06,
            3 => 1.12,
            4 => 1.17,
            _ => 1.22
        };
    }

    private static int ResolveLoopGroupMemberCount(GraphLayoutScene scene, GraphLayoutEdge edge)
    {
        int? groupId = null;
        foreach (var n in scene.Nodes)
        {
            if (n.LoopGroupId is not > 0)
                continue;
            if (string.Equals(n.Id, edge.ToNodeId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(n.Id, edge.FromNodeId, StringComparison.OrdinalIgnoreCase))
            {
                groupId = n.LoopGroupId;
                break;
            }
        }

        if (groupId is null)
            return 1;

        var count = 0;
        foreach (var n in scene.Nodes)
        {
            if (n.LoopGroupId == groupId)
                count++;
        }

        return Math.Max(1, count);
    }

    private static Pen ScaleLoopPen(Pen source, double scale, bool highlighted)
    {
        if (Math.Abs(scale - 1.0) < 0.01)
            return source;

        var thickness = source.Thickness * scale;
        IBrush brush = source.Brush ?? Brushes.White;
        if (!highlighted && brush is SolidColorBrush scb)
        {
            var c = scb.Color;
            var alphaBoost = (byte)Math.Clamp(c.A + (int)((scale - 1.0) * 48), c.A, 255);
            brush = new SolidColorBrush(Color.FromArgb(alphaBoost, c.R, c.G, c.B));
        }

        return new Pen(brush, thickness) { DashStyle = source.DashStyle, LineCap = source.LineCap };
    }

    private static void DrawBranchLabel(
        DrawingContext context,
        SkiaGraphVisualTheme theme,
        GraphLayoutScene scene,
        GraphLayoutEdge edge,
        double fromRadius,
        string label)
    {
        var dx = edge.To.X - edge.From.X;
        var dy = edge.To.Y - edge.From.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6)
            return;
        var ux = dx / len;
        var uy = dy / len;
        var start = new Point(edge.From.X + ux * fromRadius, edge.From.Y + uy * fromRadius);
        var end = edge.ToRadius is { } tr
            ? new Point(edge.To.X - ux * tr, edge.To.Y - uy * tr)
            : edge.To;
        var t = 0.38;
        var anchor = new Point(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        var px = -uy;
        var py = ux;
        var offset = 10;
        anchor = new Point(anchor.X + px * offset, anchor.Y + py * offset);

        var fontSize = scene.SideLabelFontSizePx ?? SkiaGraphRenderInvariants.MinSideLabelFontSize;
        var ft = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.SideLabelTypeface,
            Math.Max(8.5, fontSize * 0.92),
            theme.SideLabelBrush);
        context.DrawText(ft, new Point(anchor.X - ft.Width / 2, anchor.Y - ft.Height / 2));
    }

    private static double GetNodeRadius(GraphLayoutScene scene, string nodeId, double fallback)
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
        double? toRadius,
        double lateralBendMultiplier = 1.0)
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

        // Для почти вертикальных / последовательных шагов — прямая, кроме рёбер с принудительной орбитой (loop).
        var horizontalDrift = Math.Abs(ex);
        var isNearStraightFlow = horizontalDrift <= Math.Max(8, elen * 0.08);
        if (isNearStraightFlow && lateralBendMultiplier <= 1.02)
        {
            context.DrawLine(pen, start, end);
            DrawArrowHeadAtTip(context, pen, end, ex / elen, ey / elen);
            return;
        }

        var bendCap = Math.Clamp(elen * 0.22, 18, 56);
        var bendByDistance = Math.Min(bendCap, elen * 0.2);
        var bendByHorizontalRoom = Math.Max(0, horizontalDrift * 0.45);
        var crossRoom = lateralBendMultiplier > 1.02
            ? Math.Max(bendByHorizontalRoom, Math.Clamp(elen * 0.085, 12, 44))
            : bendByHorizontalRoom;
        var bend = Math.Min(bendByDistance, crossRoom) * lateralBendMultiplier;
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

using System.Windows.Input;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>Мини-карта Semantic Map: рёбра и узлы (звезда); клик по узлу открывает файл; подписи строк — в списке (режим list/both) (ADR 0039).</summary>
/// <remarks>
/// Порядок отрисовки базовой сцены (ADR 0055 §4): рёбра → узлы (фигуры и глифы) → легенда. Подсветки TraceFlow приходят в <see cref="SemanticMapGraphSceneVm"/> и рисуются вместе с рёбрами/узлами по флагам highlight.
/// </remarks>
public sealed class SemanticMapMiniMapControl : Control
{
    private const string ConditionStepKind = "condition_step";
    private const string ExitStepKind = "exit_step";
    private static readonly SemanticMapVisualTheme VisualTheme = SemanticMapVisualTheme.Default;

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
            if (!HitTestNode(n, p))
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

        // Базовая сцена: см. ADR 0055 §4 (рёбра → узлы → легенда).
        DrawEdges(context, scene);
        DrawNodes(context, scene);
        DrawLegend(context, scene, w, h);
    }

    private static bool HitTestNode(SemanticMapGraphNodeLayout n, Point p)
    {
        if (n.Shape == SemanticMapNodeShape.Diamond)
            return HitDiamond(n.Center, n.Radius, p, tolerance: 6);
        var dx = p.X - n.Center.X;
        var dy = p.Y - n.Center.Y;
        return Math.Sqrt(dx * dx + dy * dy) <= n.Radius + 6;
    }

    private static bool HitDiamond(Point center, double r, Point p, double tolerance)
    {
        var dx = Math.Abs(p.X - center.X);
        var dy = Math.Abs(p.Y - center.Y);
        return dx + dy <= r + tolerance;
    }

    private static void DrawEdges(DrawingContext context, SemanticMapGraphSceneVm scene)
    {
        var previousWasLoop = false;
        foreach (var edge in scene.Edges)
        {
            var isHighlighted = scene.HighlightedEdgeKeys.Contains(edge.Key);
            var isLoop = IsLoopEdge(edge.Kind);
            if (isLoop && !previousWasLoop)
            {
                var basePen = isHighlighted ? VisualTheme.HighlightedEdgePen : VisualTheme.BaseEdgePen;
                var loopPen = isHighlighted ? VisualTheme.HighlightedLoopEdgePen : VisualTheme.LoopEdgePen;
                DrawLoopEdge(context, scene, edge, basePen, loopPen);
                previousWasLoop = true;
                continue;
            }

            var edgeStyle = ResolveEdgePen(edge.Kind, isHighlighted);
            var fromR = GetNodeRadius(scene, edge.FromNodeId, fallback: 12);
            DrawCubicEdge(context, edgeStyle, edge.From, fromR, edge.To, edge.ToRadius);
            previousWasLoop = isLoop;
        }
    }

    private static void DrawNodes(DrawingContext context, SemanticMapGraphSceneVm scene)
    {
        var useLegend = scene.UseLegendColumn;
        foreach (var n in scene.Nodes)
        {
            var highlighted = scene.HighlightedNodeIds.Contains(n.Id);
            if (n.Shape == SemanticMapNodeShape.Diamond)
                DrawDiamondNode(context, n, highlighted);
            else
            {
                context.DrawEllipse(ResolveNodeFill(n), VisualTheme.NodeStrokePen, n.Center, n.Radius, n.Radius);
                if (highlighted)
                    context.DrawEllipse(null, VisualTheme.HighlightedNodePen, n.Center, n.Radius + 3, n.Radius + 3);
            }

            var glyph = BuildNodeGlyph(n, useLegend);
            var fontSize = Math.Max(
                SemanticMapRenderInvariants.MinGlyphFontSize,
                Math.Min(n.Radius - 1, n.LegendIndex is > 99 ? SemanticMapRenderInvariants.MinGlyphFontSize : 9));
            var glyphText = new FormattedText(
                glyph,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                VisualTheme.GlyphTypeface,
                fontSize,
                VisualTheme.GlyphBrush);
            var glyphOrigin = new Point(
                n.Center.X - glyphText.Width / 2,
                n.Center.Y - glyphText.Height / 2);
            context.DrawText(glyphText, glyphOrigin);

            var fullLabel = BuildNodeFullLabel(n, useLegend);
            if (!string.IsNullOrWhiteSpace(fullLabel))
            {
                var labelText = new FormattedText(
                    fullLabel,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    VisualTheme.SideLabelTypeface,
                    SemanticMapRenderInvariants.MinSideLabelFontSize,
                    VisualTheme.SideLabelBrush);
                var labelOrigin = new Point(
                    n.Center.X + n.Radius + 6,
                    n.Center.Y - labelText.Height / 2);
                context.DrawText(labelText, labelOrigin);
            }
        }
    }

    private static void DrawDiamondNode(DrawingContext context, SemanticMapGraphNodeLayout n, bool highlighted)
    {
        var geo = new StreamGeometry();
        var c = n.Center;
        var r = n.Radius;
        using (var ig = geo.Open())
        {
            ig.BeginFigure(new Point(c.X, c.Y - r), true);
            ig.LineTo(new Point(c.X + r, c.Y));
            ig.LineTo(new Point(c.X, c.Y + r));
            ig.LineTo(new Point(c.X - r, c.Y));
            ig.EndFigure(true);
        }

        context.DrawGeometry(ResolveNodeFill(n), VisualTheme.NodeStrokePen, geo);
        if (highlighted)
        {
            var hr = r + 3;
            var hgeo = new StreamGeometry();
            using (var ig = hgeo.Open())
            {
                ig.BeginFigure(new Point(c.X, c.Y - hr), true);
                ig.LineTo(new Point(c.X + hr, c.Y));
                ig.LineTo(new Point(c.X, c.Y + hr));
                ig.LineTo(new Point(c.X - hr, c.Y));
                ig.EndFigure(true);
            }

            context.DrawGeometry(null, VisualTheme.HighlightedNodePen, hgeo);
        }
    }

    private static void DrawLegend(DrawingContext context, SemanticMapGraphSceneVm scene, double w, double h)
    {
        if (!scene.UseLegendColumn || scene.LegendColumnLeft >= w - 24 || h < 40)
            return;

        var x0 = scene.LegendColumnLeft;
        var y = 8d;
        const double lineH = 13;
        const double keyRowH = 17d;
        const double gapBeforeKeys = 6d;
        var captionSize = SemanticMapRenderInvariants.MinLegendCaptionFontSize;
        const double colGap = 10d;

        double idxColW = 0;
        foreach (var row in scene.Legend)
        {
            var idxTxt = row.Index.ToString(CultureInfo.InvariantCulture);
            var idxFt = new FormattedText(
                idxTxt,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                VisualTheme.SideLabelTypeface,
                captionSize,
                VisualTheme.SideLabelBrush);
            idxColW = Math.Max(idxColW, idxFt.Width);
        }

        idxColW += 4;
        var textX = x0 + idxColW + colGap;
        var textMaxW = Math.Max(24, w - textX - 4);

        foreach (var row in scene.Legend)
        {
            if (y + lineH > h - 4)
                return;
            var idxTxt = row.Index.ToString(CultureInfo.InvariantCulture);
            var idxFt = new FormattedText(
                idxTxt,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                VisualTheme.SideLabelTypeface,
                captionSize,
                VisualTheme.SideLabelBrush);
            context.DrawText(idxFt, new Point(x0 + idxColW - idxFt.Width, y));

            var body = TruncateLegendCellText(row.Text, textMaxW, captionSize);
            var bodyFt = new FormattedText(
                body,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                VisualTheme.SideLabelTypeface,
                captionSize,
                VisualTheme.SideLabelBrush);
            context.DrawText(bodyFt, new Point(textX, y));
            y += lineH;
        }

        var hasShapeKeys = scene.ShowLegendReturnKey || scene.ShowLegendConditionKey;
        if (!hasShapeKeys)
            return;

        if (scene.Legend.Count > 0)
            y += gapBeforeKeys;

        if (scene.ShowLegendReturnKey)
        {
            if (y + keyRowH > h - 4)
                return;
            DrawLegendReturnKeyRow(context, x0, y, keyRowH, captionSize);
            y += keyRowH + 2;
        }

        if (scene.ShowLegendConditionKey)
        {
            if (y + keyRowH > h - 4)
                return;
            DrawLegendConditionKeyRow(context, x0, y, keyRowH, captionSize);
        }
    }

    private static void DrawLegendReturnKeyRow(DrawingContext context, double x0, double y, double rowH, double captionSize)
    {
        const double iconR = 5.5;
        var cy = y + rowH / 2;
        var cx = x0 + iconR + 1;
        context.DrawEllipse(VisualTheme.ExitFill, VisualTheme.NodeStrokePen, new Point(cx, cy), iconR, iconR);
        var arrow = new FormattedText(
            "↗",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            VisualTheme.GlyphTypeface,
            Math.Max(6, captionSize - 2),
            VisualTheme.GlyphBrush);
        context.DrawText(arrow, new Point(cx - arrow.Width / 2, cy - arrow.Height / 2));

        var cap = new FormattedText(
            "return",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            VisualTheme.SideLabelTypeface,
            captionSize,
            VisualTheme.SideLabelBrush);
        context.DrawText(cap, new Point(x0 + iconR * 2 + 10, y + (rowH - cap.Height) / 2));
    }

    private static void DrawLegendConditionKeyRow(DrawingContext context, double x0, double y, double rowH, double captionSize)
    {
        const double r = 5.5;
        var cy = y + rowH / 2;
        var cx = x0 + r + 1;
        var geo = new StreamGeometry();
        using (var ig = geo.Open())
        {
            ig.BeginFigure(new Point(cx, cy - r), true);
            ig.LineTo(new Point(cx + r, cy));
            ig.LineTo(new Point(cx, cy + r));
            ig.LineTo(new Point(cx - r, cy));
            ig.EndFigure(true);
        }

        context.DrawGeometry(VisualTheme.ConditionFill, VisualTheme.NodeStrokePen, geo);

        var cap = new FormattedText(
            "условие",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            VisualTheme.SideLabelTypeface,
            captionSize,
            VisualTheme.SideLabelBrush);
        context.DrawText(cap, new Point(x0 + r * 2 + 10, y + (rowH - cap.Height) / 2));
    }

    private static string TruncateLegendCellText(string text, double maxWidth, double fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var t = text.Replace('\r', ' ').Replace('\n', ' ');
        while (t.Contains("  ", StringComparison.Ordinal))
            t = t.Replace("  ", " ", StringComparison.Ordinal);
        t = t.Trim();
        if (t.Length > 400)
            t = t[..397] + "…";

        static double Measure(string s, double fs) =>
            new FormattedText(
                    s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    VisualTheme.SideLabelTypeface,
                    fs,
                    VisualTheme.SideLabelBrush)
                .Width;

        if (Measure(t, fontSize) <= maxWidth)
            return t;
        for (var len = t.Length - 1; len > 0; len--)
        {
            var candidate = t[..len].TrimEnd() + "…";
            if (Measure(candidate, fontSize) <= maxWidth)
                return candidate;
        }

        return "…";
    }

    private static Pen ResolveEdgePen(string? kind, bool highlighted)
    {
        if (highlighted)
            return VisualTheme.HighlightedEdgePen;
        if (IsMultiBranchEdge(kind))
            return VisualTheme.MultiBranchEdgePen;
        if (IsConditionalEdge(kind))
            return VisualTheme.ConditionalEdgePen;
        return VisualTheme.BaseEdgePen;
    }

    private static IBrush ResolveNodeFill(SemanticMapGraphNodeLayout node)
    {
        if (node.IsAnchor)
            return VisualTheme.AnchorFill;
        if (IsConditionNode(node))
            return VisualTheme.ConditionFill;
        if (IsExitNode(node))
            return VisualTheme.ExitFill;
        return VisualTheme.CallFill;
    }

    private static bool IsLoopEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("loop", StringComparison.OrdinalIgnoreCase);

    private static bool IsMultiBranchEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("multibranch", StringComparison.OrdinalIgnoreCase);

    private static void DrawLoopEdge(
        DrawingContext context,
        SemanticMapGraphSceneVm scene,
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

    /// <summary>Радиус узла-источника для обрезки (edge не хранит FromRadius).</summary>
    private static double GetNodeRadius(SemanticMapGraphSceneVm scene, string nodeId, double fallback)
    {
        foreach (var n in scene.Nodes)
        {
            if (string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase))
                return n.Radius;
        }

        return fallback;
    }

    /// <summary>Кубическая Безье между узлами: обрезка по окружностям, лёгкий изгиб (не ломаная линия через центры).</summary>
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

        var bend = Math.Min(42, elen * 0.2);
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
        // Касательная к кубической Безье в t=1: B'(1) = 3·(P₃ − P₂) — направление входа в целевой узел.
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

    /// <summary>Рисует наконечник у конца ребра; (dirX, dirY) — единичный вектор направления потока в сторону целевого узла.</summary>
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

    private static string BuildNodeGlyph(SemanticMapGraphNodeLayout node, bool useLegendColumn)
    {
        if (node.IsAnchor)
            return "A";
        // В колонке легенды у выхода — стрелка (направление «наружу»), не дублируем номер строки таблицы.
        if (useLegendColumn && IsExitNode(node))
            return "↗";
        if (node.LegendIndex is { } idx && useLegendColumn)
            return idx.ToString(CultureInfo.InvariantCulture);
        if (IsConditionNode(node))
            return "?";
        if (IsExitNode(node))
            return "↗";
        return "•";
    }

    private static string? BuildNodeFullLabel(SemanticMapGraphNodeLayout node, bool useLegendColumn)
    {
        if (useLegendColumn)
            return null;
        if (node.IsAnchor || IsConditionNode(node) || IsExitNode(node))
            return null;
        var label = node.Label?.Trim();
        if (string.IsNullOrWhiteSpace(label))
            return null;
        return label;
    }

    private static bool IsExitNode(SemanticMapGraphNodeLayout node) =>
        string.Equals(node.Kind, ExitStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionNode(SemanticMapGraphNodeLayout node) =>
        string.Equals(node.Kind, ConditionStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionalEdge(string? kind) =>
        !string.IsNullOrWhiteSpace(kind)
        && kind.Contains("conditional", StringComparison.OrdinalIgnoreCase);
}

using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

public static partial class SemanticMapSceneDrawing
{
    private static void DrawLegend(DrawingContext context, SemanticMapGraphSceneVm scene, SemanticMapVisualTheme theme, double w, double h)
    {
        if (!scene.UseLegendColumn || h < 40)
            return;

        var isBelow = scene.LegendPlacement == SemanticMapLegendBlockPlacement.BelowGraph;
        if (isBelow)
        {
            if (scene.LegendBlockTopY <= 0 || scene.LegendBlockTopY >= h - 12)
                return;
        }
        else if (scene.LegendColumnLeft >= w - 24)
        {
            return;
        }

        var x0 = scene.LegendColumnLeft;
        var y = isBelow ? scene.LegendBlockTopY : 8d;
        var legendViewportH = h - y - 4;
        if (legendViewportH < 12)
            return;

        var captionSize = scene.SideLabelFontSizePx is { } s
            ? Math.Clamp(s, SemanticMapRenderInvariants.MinLegendCaptionFontSize, SemanticMapRenderInvariants.MaxSideLabelFontSize)
            : 12;

        var idxColW = MeasureIndexColumnWidth(theme, scene.Legend, captionSize);
        const double colGap = 6d;
        var textX = x0 + idxColW + colGap;
        var textMaxW = Math.Max(24, w - textX - 4);

        captionSize = FitLegendCaptionSize(theme, scene.Legend, textMaxW, captionSize, legendViewportH);

        var lineH = Math.Max(15, captionSize * 1.2);
        var keyRowH = Math.Max(17d, captionSize + 5);
        const double gapBeforeKeys = 6d;

        foreach (var row in scene.Legend)
        {
            if (y + lineH > h - 4)
                return;
            var idxTxt = row.Index.ToString(CultureInfo.InvariantCulture);
            var idxFt = new FormattedText(
                idxTxt,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                theme.SideLabelTypeface,
                captionSize,
                theme.SideLabelBrush);
            context.DrawText(idxFt, new Point(x0 + idxColW - idxFt.Width, y));

            var body = row.Text.Replace('\r', ' ').Replace('\n', ' ');
            while (body.Contains("  ", StringComparison.Ordinal))
                body = body.Replace("  ", " ", StringComparison.Ordinal);
            body = body.Trim();
            if (body.Length > 400)
                body = body[..397] + "…";

            var bodyFt = new FormattedText(
                body,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                theme.SideLabelTypeface,
                captionSize,
                theme.SideLabelBrush);
            context.DrawText(bodyFt, new Point(textX, y));
            y += lineH;
        }

        var hasShapeKeys = scene.ShowLegendReturnKey || scene.ShowLegendConditionKey || scene.ShowLegendExceptionFlowKey;
        var hasAnyKeys = hasShapeKeys || scene.ShowLegendEdgeStyleKey;
        if (!hasAnyKeys)
            return;

        if (scene.Legend.Count > 0 && (scene.ShowLegendEdgeStyleKey || hasShapeKeys))
            y += gapBeforeKeys;

        if (scene.ShowLegendEdgeStyleKey)
        {
            y = DrawLegendEdgeStyleKeyBlock(
                context,
                theme,
                x0,
                y,
                keyRowH,
                captionSize,
                h,
                scene.Edges);
        }

        if (hasShapeKeys && scene.ShowLegendEdgeStyleKey)
            y += gapBeforeKeys;

        if (scene.ShowLegendReturnKey)
        {
            if (y + keyRowH > h - 4)
                return;
            DrawLegendReturnKeyRow(context, theme, x0, y, keyRowH, captionSize);
            y += keyRowH + 2;
        }

        if (scene.ShowLegendConditionKey)
        {
            if (y + keyRowH > h - 4)
                return;
            DrawLegendConditionKeyRow(context, theme, x0, y, keyRowH, captionSize);
            y += keyRowH + 2;
        }

        if (scene.ShowLegendExceptionFlowKey)
        {
            if (y + keyRowH > h - 4)
                return;
            DrawLegendExceptionFlowKeyRow(context, theme, x0, y, keyRowH, captionSize);
        }
    }

    private static double MeasureIndexColumnWidth(SemanticMapVisualTheme theme, IReadOnlyList<SemanticMapLegendEntry> rows, double captionSize)
    {
        var idxColW = 0d;
        foreach (var row in rows)
        {
            var idxTxt = row.Index.ToString(CultureInfo.InvariantCulture);
            var idxFt = new FormattedText(
                idxTxt,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                theme.SideLabelTypeface,
                captionSize,
                theme.SideLabelBrush);
            idxColW = Math.Max(idxColW, idxFt.Width);
        }

        return idxColW + 4;
    }

    private static double FitLegendCaptionSize(
        SemanticMapVisualTheme theme,
        IReadOnlyList<SemanticMapLegendEntry> rows,
        double textMaxW,
        double captionSize,
        double viewportH)
    {
        var size = captionSize;
        const double floor = 9;
        while (size >= floor)
        {
            var lineH = Math.Max(15, size * 1.2);
            var used = 8d + rows.Count * lineH;
            if (used > viewportH - 8 && rows.Count > 0)
            {
                size -= 0.5;
                continue;
            }

            var fits = true;
            foreach (var row in rows)
            {
                var t = row.Text.Replace('\r', ' ').Replace('\n', ' ');
                while (t.Contains("  ", StringComparison.Ordinal))
                    t = t.Replace("  ", " ", StringComparison.Ordinal);
                t = t.Trim();
                if (t.Length > 400)
                    t = t[..397] + "…";
                var w = new FormattedText(
                    t,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    theme.SideLabelTypeface,
                    size,
                    theme.SideLabelBrush).Width;
                if (w > textMaxW)
                {
                    fits = false;
                    break;
                }
            }

            if (fits)
                return size;
            size -= 0.5;
        }

        return floor;
    }

    private static void DrawLegendReturnKeyRow(DrawingContext context, SemanticMapVisualTheme theme, double x0, double y, double rowH, double captionSize)
    {
        const double iconR = 5.5;
        var cy = y + rowH / 2;
        var cx = x0 + iconR + 1;
        context.DrawEllipse(theme.ExitFill, theme.NodeStrokePen, new Point(cx, cy), iconR, iconR);
        var arrowLen = Math.Max(4.5, Math.Min(iconR * 1.35, captionSize * 0.55));
        DrawNorthEastExitArrowShaftCentered(context, theme.GlyphBrush, new Point(cx, cy), arrowLen, 1.2);

        var cap = new FormattedText(
            "return",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.SideLabelTypeface,
            captionSize,
            theme.SideLabelBrush);
        context.DrawText(cap, new Point(x0 + iconR * 2 + 10, y + (rowH - cap.Height) / 2));
    }

    private static void DrawLegendConditionKeyRow(DrawingContext context, SemanticMapVisualTheme theme, double x0, double y, double rowH, double captionSize)
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

        context.DrawGeometry(theme.ConditionFill, theme.NodeStrokePen, geo);

        var cap = new FormattedText(
            "условие",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.SideLabelTypeface,
            captionSize,
            theme.SideLabelBrush);
        context.DrawText(cap, new Point(x0 + r * 2 + 10, y + (rowH - cap.Height) / 2));
    }

    private static void DrawLegendExceptionFlowKeyRow(DrawingContext context, SemanticMapVisualTheme theme, double x0, double y, double rowH, double captionSize)
    {
        const double iconR = 5.5;
        var cy = y + rowH / 2;
        var cx = x0 + iconR + 1;
        context.DrawEllipse(theme.HandlerFill, theme.NodeStrokePen, new Point(cx, cy), iconR, iconR);
        var ex = new FormattedText(
            "!",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.GlyphTypeface,
            Math.Max(7, captionSize - 1),
            theme.GlyphBrush);
        context.DrawText(ex, new Point(cx - ex.Width / 2, cy - ex.Height / 2));

        var cap = new FormattedText(
            "catch / handler",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.SideLabelTypeface,
            captionSize,
            theme.SideLabelBrush);
        context.DrawText(cap, new Point(x0 + iconR * 2 + 10, y + (rowH - cap.Height) / 2));
    }

    private static double DrawLegendEdgeStyleKeyBlock(
        DrawingContext context,
        SemanticMapVisualTheme theme,
        double x0,
        double y,
        double keyRowH,
        double captionSize,
        double viewportBottom,
        IReadOnlyList<SemanticMapGraphEdgeLayout> edges)
    {
        static bool KindContains(string? kind, string needle) =>
            !string.IsNullOrEmpty(kind) && kind.Contains(needle, StringComparison.OrdinalIgnoreCase);

        var hasNonSolid = edges.Any(e =>
            KindContains(e.Kind, "conditional")
            || KindContains(e.Kind, "exception")
            || KindContains(e.Kind, "multibranch")
            || KindContains(e.Kind, "loop"));
        if (!hasNonSolid)
            return y;

        const double lineLen = 34d;
        var lineLeft = x0 + 2;
        var textX = lineLeft + lineLen + 10;

        void OneRow(Pen pen, string text, ref double yRow)
        {
            if (yRow + keyRowH > viewportBottom - 4)
                return;
            var cy = yRow + keyRowH / 2;
            context.DrawLine(
                pen,
                new Point(lineLeft, cy),
                new Point(lineLeft + lineLen, cy));
            var cap = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                theme.SideLabelTypeface,
                captionSize,
                theme.SideLabelBrush);
            context.DrawText(cap, new Point(textX, yRow + (keyRowH - cap.Height) / 2));
            yRow += keyRowH + 2;
        }

        // Совпадает с <see cref="SemanticMapSceneDrawing.Edges"/>: solid — основной поток.
        OneRow(theme.BaseEdgePen, "основной поток (вызовы, return, последовательно)", ref y);
        if (edges.Any(e => KindContains(e.Kind, "conditional") || KindContains(e.Kind, "exception")))
            OneRow(theme.ConditionalEdgePen, "длинные штрихи — ветвления, catch, исключения", ref y);
        if (edges.Any(e => KindContains(e.Kind, "multibranch")))
            OneRow(theme.MultiBranchEdgePen, "короткие штрихи — switch", ref y);
        if (edges.Any(e => KindContains(e.Kind, "loop")))
            OneRow(theme.LoopEdgePen, "толстая линия / кольцо — тело цикла", ref y);
        return y;
    }
}

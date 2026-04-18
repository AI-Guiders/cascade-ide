using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

public static partial class SemanticMapSceneDrawing
{
    private static void DrawLegend(DrawingContext context, SemanticMapGraphSceneVm scene, SemanticMapVisualTheme theme, double w, double h)
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
                theme.SideLabelTypeface,
                captionSize,
                theme.SideLabelBrush);
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
                theme.SideLabelTypeface,
                captionSize,
                theme.SideLabelBrush);
            context.DrawText(idxFt, new Point(x0 + idxColW - idxFt.Width, y));

            var body = TruncateLegendCellText(theme, row.Text, textMaxW, captionSize);
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

        var hasShapeKeys = scene.ShowLegendReturnKey || scene.ShowLegendConditionKey;
        if (!hasShapeKeys)
            return;

        if (scene.Legend.Count > 0)
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
        }
    }

    private static void DrawLegendReturnKeyRow(DrawingContext context, SemanticMapVisualTheme theme, double x0, double y, double rowH, double captionSize)
    {
        const double iconR = 5.5;
        var cy = y + rowH / 2;
        var cx = x0 + iconR + 1;
        context.DrawEllipse(theme.ExitFill, theme.NodeStrokePen, new Point(cx, cy), iconR, iconR);
        var arrow = new FormattedText(
            "↗",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.GlyphTypeface,
            Math.Max(6, captionSize - 2),
            theme.GlyphBrush);
        context.DrawText(arrow, new Point(cx - arrow.Width / 2, cy - arrow.Height / 2));

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

    private static string TruncateLegendCellText(SemanticMapVisualTheme theme, string text, double maxWidth, double fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var t = text.Replace('\r', ' ').Replace('\n', ' ');
        while (t.Contains("  ", StringComparison.Ordinal))
            t = t.Replace("  ", " ", StringComparison.Ordinal);
        t = t.Trim();
        if (t.Length > 400)
            t = t[..397] + "…";

        static double Measure(SemanticMapVisualTheme th, string s, double fs) =>
            new FormattedText(
                    s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    th.SideLabelTypeface,
                    fs,
                    th.SideLabelBrush)
                .Width;

        if (Measure(theme, t, fontSize) <= maxWidth)
            return t;
        for (var len = t.Length - 1; len > 0; len--)
        {
            var candidate = t[..len].TrimEnd() + "…";
            if (Measure(theme, candidate, fontSize) <= maxWidth)
                return candidate;
        }

        return "…";
    }
}

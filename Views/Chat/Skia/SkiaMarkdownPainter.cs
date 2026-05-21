#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal static class SkiaMarkdownPainter
{
    public static float MeasureHeight(IReadOnlyList<SkiaMarkdownRow> rows, bool compact) =>
        rows.Count == 0 ? 0f : rows.Sum(r => RowHeight(r.Kind, compact));

    public static float Draw(
        SkiaChatDrawContext context,
        float left,
        float right,
        float top,
        IReadOnlyList<SkiaMarkdownRow> rows,
        bool compact,
        SKColor bodyColor) =>
        Draw(context, left, right, top, rows, compact, bodyColor, SkiaChatFeedLayout.For(compact));

    public static float Draw(
        SkiaChatDrawContext context,
        float left,
        float right,
        float top,
        IReadOnlyList<SkiaMarkdownRow> rows,
        bool compact,
        SKColor bodyColor,
        in SkiaChatFeedLayout layout)
    {
        var y = top;
        foreach (var row in rows)
        {
            y += DrawRow(context, left, right, y, row, compact, bodyColor, layout);
        }

        return y;
    }

    private static float DrawRow(
        SkiaChatDrawContext context,
        float left,
        float right,
        float baselineY,
        SkiaMarkdownRow row,
        bool compact,
        SKColor bodyColor,
        in SkiaChatFeedLayout layout)
    {
        var height = RowHeight(row.Kind, compact);
        if (row.Kind == SkiaMarkdownBlockKind.HorizontalRule)
        {
            var midY = baselineY + height * 0.45f;
            using var paint = new SKPaint
            {
                Color = SkiaKitColor.Blend(context.Theme.Border, context.Theme.Content, 0.35f),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
            };
            context.Canvas.DrawLine(left, midY, right, midY, paint);
            return height;
        }

        if (row.Kind == SkiaMarkdownBlockKind.Blank || row.Runs.Count == 0)
            return height;

        var (bodySize, boldSize) = BodySizes(row.Kind, compact);
        using var bodyFont = SkiaChatFeedFontResolver.CreateFont(layout.ProseFamily, bodySize);
        var textY = baselineY + BaselineOffset(row.Kind, compact);

        var x = left;
        foreach (var run in row.Runs)
        {
            if (run.Text.Length == 0)
                continue;

            var (font, color, disposeFont) = ResolveRunStyle(
                context,
                layout,
                bodyFont,
                bodySize,
                boldSize,
                run.Style,
                row.Kind,
                bodyColor);
            try
            {
                using var paint = new SKPaint { IsAntialias = true, Color = color };
                context.Canvas.DrawText(run.Text, x, textY, SKTextAlign.Left, font, paint);
                x += font.MeasureText(run.Text);
            }
            finally
            {
                if (disposeFont)
                    font.Dispose();
            }
        }

        return height;
    }

    private static (SKFont Font, SKColor Color, bool DisposeFont) ResolveRunStyle(
        SkiaChatDrawContext ctx,
        in SkiaChatFeedLayout layout,
        SKFont bodyFont,
        float bodySize,
        float boldSize,
        SkiaMarkdownStyle style,
        SkiaMarkdownBlockKind block,
        SKColor bodyColor)
    {
        var heading = block is SkiaMarkdownBlockKind.Heading1
            or SkiaMarkdownBlockKind.Heading2
            or SkiaMarkdownBlockKind.Heading3;

        if (heading && style is SkiaMarkdownStyle.Plain or SkiaMarkdownStyle.Bold)
            return (
                SkiaChatFeedFontResolver.CreateFont(layout.ProseFamily, boldSize, SKFontStyle.Bold),
                bodyColor,
                true);

        return style switch
        {
            SkiaMarkdownStyle.Bold => (
                SkiaChatFeedFontResolver.CreateFont(layout.ProseFamily, bodySize, SKFontStyle.Bold),
                bodyColor,
                true),
            SkiaMarkdownStyle.Italic => (
                SkiaChatFeedFontResolver.CreateFont(layout.ProseFamily, bodySize, SKFontStyle.Italic),
                bodyColor,
                true),
            SkiaMarkdownStyle.Code => (
                SkiaChatFeedFontResolver.CreateFont(layout.MonoFamily, bodySize * 0.95f),
                SkiaKitColor.Blend(bodyColor, ctx.Theme.HoverBorder, 0.35f),
                true),
            _ => (bodyFont, bodyColor, false),
        };
    }

    private static float RowHeight(SkiaMarkdownBlockKind kind, bool compact) =>
        kind switch
        {
            SkiaMarkdownBlockKind.Heading1 => compact ? 20f : 22f,
            SkiaMarkdownBlockKind.Heading2 => compact ? 17f : 19f,
            SkiaMarkdownBlockKind.Heading3 => compact ? 15.5f : 17f,
            SkiaMarkdownBlockKind.HorizontalRule => compact ? 10f : 12f,
            SkiaMarkdownBlockKind.Blank => compact ? 5f : 6f,
            _ => compact ? 14f : 15f,
        };

    private static float BaselineOffset(SkiaMarkdownBlockKind kind, bool compact) =>
        kind switch
        {
            SkiaMarkdownBlockKind.Heading1 => compact ? 14f : 16f,
            SkiaMarkdownBlockKind.Heading2 => compact ? 12.5f : 14f,
            SkiaMarkdownBlockKind.Heading3 => compact ? 11.5f : 13f,
            _ => compact ? 11f : 12f,
        };

    private static (float BodySize, float BoldSize) BodySizes(SkiaMarkdownBlockKind kind, bool compact) =>
        kind switch
        {
            SkiaMarkdownBlockKind.Heading1 => compact ? (12f, 13f) : (13f, 14f),
            SkiaMarkdownBlockKind.Heading2 => compact ? (11.5f, 12f) : (12f, 12.5f),
            SkiaMarkdownBlockKind.Heading3 => compact ? (11f, 11.5f) : (11.5f, 12f),
            _ => compact ? (11f, 11f) : (11.5f, 11.5f),
        };
}

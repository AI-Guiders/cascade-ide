using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Нумерация сообщений слева от ленты (detail thread).</summary>
internal static class SkiaChatFeedGutter
{
    public static void DrawOrdinal(
        SkiaChatDrawContext context,
        float top,
        float bottom,
        int ordinal,
        in SkiaChatFeedLayout layout,
        bool isSelected)
    {
        if (ordinal <= 0)
            return;

        var gutterRect = new SKRect(context.FeedGutterLeft, top, context.ContentLeft, bottom);
        using var numFont = SkiaChatFeedFontResolver.CreateFont(layout.GutterFamily, layout.GutterOrdinalFontSize);
        using var numPaint = new SKPaint
        {
            IsAntialias = true,
            Color = isSelected
                ? context.Theme.SelectedBorder
                : SkiaKit.SkiaKitColor.Blend(context.Theme.Role, context.Theme.Content, 0.55f),
        };
        var baseline = top + numFont.Size * SkiaChatFeedLayout.TextBaselineFactor + layout.RowEdgePad;
        context.Canvas.DrawText(
            ordinal.ToString(),
            gutterRect.Right - layout.TextInset,
            baseline,
            SKTextAlign.Right,
            numFont,
            numPaint);
    }
}

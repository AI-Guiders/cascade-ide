using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Колонка роли слева от тела сообщения — метрики из <see cref="SkiaChatFeedLayout"/>.</summary>
internal static class SkiaChatFeedRoleRail
{
    public static void Draw(
        SKCanvas canvas,
        SkiaChatTheme theme,
        float railLeft,
        float top,
        float bottom,
        string roleLabel,
        in SkiaChatFeedLayout layout)
    {
        if (string.IsNullOrWhiteSpace(roleLabel))
            return;

        var railWidth = layout.RoleRailWidth;
        var railRect = new SKRect(railLeft, top, railLeft + railWidth, bottom);
        canvas.Save();
        canvas.ClipRect(railRect);

        using var rulePaint = new SKPaint
        {
            Color = theme.Border.WithAlpha(90),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };
        canvas.DrawLine(railRect.Right, railRect.Top + layout.RowEdgePad, railRect.Right, railRect.Bottom - layout.RowEdgePad, rulePaint);

        using var font = SkiaChatFeedFontResolver.CreateFont(
            layout.RoleFamily,
            layout.RoleLabelFontSize,
            SKFontStyle.Bold);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = theme.Role.WithAlpha(210),
        };
        var baseline = layout.RoleLabelBaselineY(top);
        var text = TruncateToWidth(roleLabel, font, layout.RoleLabelMaxWidth);
        canvas.DrawText(text, railLeft + layout.RoleLabelInset, baseline, SKTextAlign.Left, font, paint);
        canvas.Restore();
    }

    private static string TruncateToWidth(string text, SKFont font, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || font.MeasureText(text) <= maxWidth)
            return text;

        const string ellipsis = "…";
        var ellipsisWidth = font.MeasureText(ellipsis);
        var maxBody = Math.Max(0f, maxWidth - ellipsisWidth);
        for (var len = text.Length; len > 0; len--)
        {
            var slice = text[..len];
            if (font.MeasureText(slice) <= maxBody)
                return slice + ellipsis;
        }

        return ellipsis;
    }
}

#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Skia toolbar Intercom (ADR 0123 фаза 1).</summary>
internal static class SkiaChatChromeRenderer
{
    public const float ToolbarHeight = 36f;

    public static void Draw(
        SKCanvas canvas,
        float width,
        SkiaChatTheme theme,
        string title,
        bool overviewMode,
        bool isLoading,
        string? loadingText,
        out SKRect overviewButtonBounds)
    {
        canvas.DrawRect(new SKRect(0, 0, width, ToolbarHeight), new SKPaint
        {
            Color = theme.Surface,
            IsStroke = false,
        });
        canvas.DrawLine(0, ToolbarHeight - 0.5f, width, ToolbarHeight - 0.5f, new SKPaint
        {
            Color = theme.Border,
            StrokeWidth = 1,
            IsAntialias = true,
        });

        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 13);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
        canvas.DrawText(title, 12, 23, SKTextAlign.Left, titleFont, titlePaint);

        var topicsLabel = overviewMode ? "Темы ✓" : "Темы";
        using var btnFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
        using var btnPaint = new SKPaint { IsAntialias = true, Color = theme.Role };
        var topicsWidth = btnFont.MeasureText(topicsLabel) + 16;
        var right = width - 12f;
        overviewButtonBounds = new SKRect(right - topicsWidth, 6, right, ToolbarHeight - 6);
        right = overviewButtonBounds.Left - 8f;
        if (isLoading && !string.IsNullOrWhiteSpace(loadingText))
        {
            using var chipFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
            var chipText = Truncate(loadingText, 28);
            var chipWidth = chipFont.MeasureText(chipText) + 14;
            var chipRect = new SKRect(right - chipWidth, 8, right, ToolbarHeight - 8);
            DrawChip(canvas, chipRect, chipText, theme, chipFont);
        }

        using var btnBg = new SKPaint { IsAntialias = true, Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.35f) };
        canvas.DrawRoundRect(overviewButtonBounds, 6, 6, btnBg);
        canvas.DrawText(topicsLabel, overviewButtonBounds.MidX, 22, SKTextAlign.Center, btnFont, btnPaint);
    }

    private static void DrawChip(SKCanvas canvas, SKRect rect, string text, SkiaChatTheme theme, SKFont font)
    {
        using var bg = new SKPaint { IsAntialias = true, Color = SkiaKit.SkiaKitColor.Blend(theme.BubbleAssistant, theme.Border, 0.25f) };
        canvas.DrawRoundRect(rect, 8, 8, bg);
        using var paint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
        canvas.DrawText(text, rect.Left + 7, 21, SKTextAlign.Left, font, paint);
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "…";
}

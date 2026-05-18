#nullable enable
using CascadeIDE.Features.Chat;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Skia toolbar Intercom (ADR 0123 фаза 1).</summary>
internal static class SkiaChatChromeRenderer
{
    public const float ToolbarHeight = 36f;

    /// <summary>Toolbar с подзаголовком (тема / линия / сообщений).</summary>
    public const float ToolbarWithStatusHeight = 52f;

    /// <summary>Заголовок режима overview (картотека) — не в скролле ленты.</summary>
    public const float OverviewCatalogBandHeight = 44f;

    public static float ResolveToolbarHeight(bool compactLayout, bool showStatusSubtitle) =>
        compactLayout ? (showStatusSubtitle ? ToolbarWithStatusHeight : ToolbarHeight) : 0f;

    public static float ResolveTopChromeHeight(bool compactLayout, bool showOverviewCatalog, bool showStatusSubtitle) =>
        ResolveToolbarHeight(compactLayout, showStatusSubtitle)
        + (showOverviewCatalog ? OverviewCatalogBandHeight : 0f);

    public static void Draw(
        SKCanvas canvas,
        float width,
        SkiaChatTheme theme,
        string title,
        bool overviewMode,
        bool isLoading,
        string? loadingText,
        string? statusSubtitle,
        out SKRect overviewButtonBounds)
    {
        var toolbarHeight = ResolveToolbarHeight(compactLayout: true, showStatusSubtitle: !string.IsNullOrWhiteSpace(statusSubtitle));
        canvas.DrawRect(new SKRect(0, 0, width, toolbarHeight), new SKPaint
        {
            Color = theme.Surface,
            IsStroke = false,
        });
        canvas.DrawLine(0, toolbarHeight - 0.5f, width, toolbarHeight - 0.5f, new SKPaint
        {
            Color = theme.Border,
            StrokeWidth = 1,
            IsAntialias = true,
        });

        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 13);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
        canvas.DrawText(title, 12, 20, SKTextAlign.Left, titleFont, titlePaint);

        if (!string.IsNullOrWhiteSpace(statusSubtitle))
        {
            using var statusFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
            using var statusPaint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
            canvas.DrawText(
                Truncate(statusSubtitle, 96),
                12,
                38,
                SKTextAlign.Left,
                statusFont,
                statusPaint);
        }

        var topicsLabel = overviewMode ? "Темы ✓" : "Темы";
        using var btnFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
        using var btnPaint = new SKPaint { IsAntialias = true, Color = theme.Role };
        var topicsWidth = btnFont.MeasureText(topicsLabel) + 16;
        var right = width - 12f;
        overviewButtonBounds = new SKRect(right - topicsWidth, 6, right, 28);
        right = overviewButtonBounds.Left - 8f;
        if (isLoading && !string.IsNullOrWhiteSpace(loadingText))
        {
            using var chipFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
            var chipText = Truncate(loadingText, 28);
            var chipWidth = chipFont.MeasureText(chipText) + 14;
            var chipRect = new SKRect(right - chipWidth, 8, right, 28);
            DrawChip(canvas, chipRect, chipText, theme, chipFont);
        }

        using var btnBg = new SKPaint { IsAntialias = true, Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.35f) };
        canvas.DrawRoundRect(overviewButtonBounds, 6, 6, btnBg);
        canvas.DrawText(topicsLabel, overviewButtonBounds.MidX, 20, SKTextAlign.Center, btnFont, btnPaint);
    }

    /// <summary>Полоса «Картотека тем» под toolbar — заголовок режима, не карточка в ленте.</summary>
    public static void DrawOverviewCatalogBand(SKCanvas canvas, float width, float top, SkiaChatTheme theme, int topicCount)
    {
        var bottom = top + OverviewCatalogBandHeight;
        using (var fill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.08f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        })
            canvas.DrawRect(new SKRect(0, top, width, bottom), fill);

        using (var line = new SKPaint
        {
            Color = theme.Border,
            StrokeWidth = 1,
            IsAntialias = true,
        })
            canvas.DrawLine(0, bottom - 0.5f, width, bottom - 0.5f, line);

        using var modeFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 14);
        using var modePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
        canvas.DrawText("Картотека тем", 12, top + 20, SKTextAlign.Left, modeFont, modePaint);

        var hint = ChatThreadOverviewPresentation.FormatCatalogHint(topicCount) + " · " +
                   ChatThreadOverviewPresentation.CatalogFooter;
        using var hintFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
        using var hintPaint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
        canvas.DrawText(hint, 12, top + 36, SKTextAlign.Left, hintFont, hintPaint);
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

#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Services;
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

    public static float ResolveToolbarHeight(bool forwardHost, bool showStatusSubtitle) =>
        forwardHost ? (showStatusSubtitle ? ToolbarWithStatusHeight : ToolbarHeight) : 0f;

    public static float ResolveTopChromeHeight(bool forwardHost, bool showOverviewCatalog, bool showStatusSubtitle) =>
        ResolveTopChromeHeight(forwardHost, showOverviewCatalog, showStatusSubtitle, overviewMode: showOverviewCatalog, topicCount: 0);

    public static float ResolveTopChromeHeight(
        bool forwardHost,
        bool showOverviewCatalog,
        bool showStatusSubtitle,
        bool overviewMode,
        int topicCount) =>
        SkiaIntercomNavigationChrome.ResolveTopChromeHeight(
            forwardHost,
            showOverviewCatalog,
            showStatusSubtitle,
            overviewMode,
            topicCount);

    public static void Draw(
        SKCanvas canvas,
        float width,
        SkiaChatTheme theme,
        string title,
        bool overviewMode,
        bool isLoading,
        string? loadingText,
        string? statusSubtitle,
        bool showNavigatorToggle,
        bool navigatorVisible,
        out SKRect overviewButtonBounds,
        out SKRect navigatorToggleBounds,
        IntercomFontsSettings? fonts = null)
    {
        navigatorToggleBounds = default;
        fonts ??= IntercomFontDefaults.Intercom;
        var titlePt = fonts.ResolveChromeTitlePt();
        var subtitlePt = fonts.ResolveChromeSubtitlePt();
        var toolbarHeight = ResolveToolbarHeight(forwardHost: true, showStatusSubtitle: !string.IsNullOrWhiteSpace(statusSubtitle));
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

        using var titleFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily(), SKFontStyle.Bold), titlePt);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
        canvas.DrawText(title, 12, 12 + titlePt * 0.85f, SKTextAlign.Left, titleFont, titlePaint);

        if (!string.IsNullOrWhiteSpace(statusSubtitle))
        {
            using var statusFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), subtitlePt);
            using var statusPaint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
            canvas.DrawText(
                Truncate(statusSubtitle, 96),
                12,
                12 + titlePt * 0.85f + subtitlePt + 6f,
                SKTextAlign.Left,
                statusFont,
                statusPaint);
        }

        var topicsLabel = overviewMode ? "Темы ✓" : "Темы";
        var btnPt = Math.Max(10f, subtitlePt + 1f);
        using var btnFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), btnPt);
        using var btnPaint = new SKPaint { IsAntialias = true, Color = theme.Role };
        var topicsWidth = btnFont.MeasureText(topicsLabel) + 16;
        var right = width - 12f;
        overviewButtonBounds = new SKRect(right - topicsWidth, 6, right, 28);
        right = overviewButtonBounds.Left - 8f;

        if (showNavigatorToggle)
        {
            var navLabel = navigatorVisible ? "Nav ✓" : "☰ Nav";
            var navWidth = btnFont.MeasureText(navLabel) + 16;
            navigatorToggleBounds = new SKRect(right - navWidth, 6, right, 28);
            right = navigatorToggleBounds.Left - 8f;
            using var navBg = new SKPaint { IsAntialias = true, Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.35f) };
            canvas.DrawRoundRect(navigatorToggleBounds, 6, 6, navBg);
            canvas.DrawText(navLabel, navigatorToggleBounds.MidX, 20, SKTextAlign.Center, btnFont, btnPaint);
        }
        if (isLoading && !string.IsNullOrWhiteSpace(loadingText))
        {
            using var chipFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), subtitlePt);
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
    public static void DrawOverviewCatalogBand(
        SKCanvas canvas,
        float width,
        float top,
        SkiaChatTheme theme,
        int topicCount,
        IntercomFontsSettings? fonts = null)
    {
        fonts ??= IntercomFontDefaults.Intercom;
        var headingPt = fonts.ResolveChromeHeadingPt();
        var hintPt = fonts.ResolveChromeSubtitlePt();
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

        using var modeFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily(), SKFontStyle.Bold), headingPt);
        using var modePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
        canvas.DrawText("Картотека тем", 12, top + headingPt * 0.85f + 6f, SKTextAlign.Left, modeFont, modePaint);

        var hint = ChatThreadOverviewPresentation.FormatCatalogHint(topicCount) + " · " +
                   ChatThreadOverviewPresentation.CatalogFooter;
        using var hintFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), hintPt);
        using var hintPaint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
        canvas.DrawText(hint, 12, top + headingPt * 0.85f + hintPt + 14f, SKTextAlign.Left, hintFont, hintPaint);
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

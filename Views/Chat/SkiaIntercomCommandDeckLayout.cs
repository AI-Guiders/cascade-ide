#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat;

/// <summary>
/// Command Deck: composer + CCL + slash popup в одном нижнем блоке (ADR 0138).
/// Подсказки не рисуются над лентой — только внутри deck, лента отступает на <see cref="Layout.TotalHeight"/>.
/// </summary>
internal static class SkiaIntercomCommandDeckLayout
{
    public const float SectionGap = 2f;
    public const float PopupHorizontalInset = 8f;
    public const float PopupInnerPadding = 4f;

    public readonly record struct Layout(
        float TotalHeight,
        SKRect DeckBounds,
        SKRect ComposerBounds,
        SKRect CommandLineBounds,
        SKRect SlashPopupBounds)
    {
        public bool HasDeck => DeckBounds.Width > 0 && DeckBounds.Height > 0;
    }

    public static Layout Compute(
        float surfaceWidth,
        float surfaceBottom,
        bool showComposer,
        bool showCommandLine,
        string? commandLinePreview,
        string composerText,
        bool showSlashPopup,
        int slashRowCount,
        string? composerPreeditText = null,
        bool showSlashHierarchyHeader = false,
        float composerFontSize = 12f,
        float composerLineHeight = 17f,
        string? composerSlashPreview = null,
        float composerSlashPreviewFontSize = 10f,
        float commandLineFontSize = 12f,
        float commandLinePreviewFontSize = 10f)
    {
        if (!showComposer || surfaceWidth <= 0f)
            return default;

        var contentWidth = Math.Max(
            40f,
            surfaceWidth - SkiaComposerStrip.HorizontalPadding * 2 - SkiaComposerStrip.SendButtonWidth - 24f);
        var composerHeight = SkiaComposerStrip.MeasureHeight(
            composerText,
            composerPreeditText,
            contentWidth,
            composerFontSize,
            composerLineHeight,
            composerSlashPreview,
            composerSlashPreviewFontSize);
        var commandLineHeight = showCommandLine
            ? SkiaCommandLineStrip.MeasureHeight(commandLinePreview, commandLineFontSize, commandLinePreviewFontSize)
            : 0f;
        var popupHeight = showSlashPopup && slashRowCount > 0
            ? SkiaPopupList.MeasureHeight(slashRowCount, showSlashHierarchyHeader) + PopupInnerPadding
            : 0f;

        var cursor = surfaceBottom;
        var composerTop = cursor - composerHeight;
        var composerBounds = new SKRect(0f, composerTop, surfaceWidth, cursor);
        cursor = composerTop - SectionGap;

        var commandLineBounds = default(SKRect);
        if (commandLineHeight > 0f)
        {
            var cclBottom = cursor;
            var cclTop = cclBottom - commandLineHeight;
            commandLineBounds = new SKRect(0f, cclTop, surfaceWidth, cclBottom);
            cursor = cclTop - SectionGap;
        }

        var slashPopupBounds = default(SKRect);
        float deckTop;
        if (popupHeight > 0f)
        {
            var popupBottom = cursor;
            var popupTop = popupBottom - popupHeight;
            slashPopupBounds = new SKRect(
                PopupHorizontalInset,
                popupTop,
                surfaceWidth - PopupHorizontalInset,
                popupBottom);
            deckTop = popupTop;
        }
        else
            deckTop = commandLineHeight > 0f ? commandLineBounds.Top : composerBounds.Top;

        var bottom = surfaceBottom;
        var deckBounds = new SKRect(0f, deckTop, surfaceWidth, bottom);
        var totalHeight = bottom - deckTop;
        return new Layout(totalHeight, deckBounds, composerBounds, commandLineBounds, slashPopupBounds);
    }

    public static float MeasureTotalHeight(
        float surfaceWidth,
        bool showComposer,
        bool showCommandLine,
        string? commandLinePreview,
        string composerText,
        bool showSlashPopup,
        int slashRowCount,
        string? composerPreeditText = null,
        bool showSlashHierarchyHeader = false,
        float composerFontSize = 12f,
        float composerLineHeight = 17f,
        string? composerSlashPreview = null,
        float composerSlashPreviewFontSize = 10f,
        float commandLineFontSize = 12f,
        float commandLinePreviewFontSize = 10f) =>
        Compute(
                surfaceWidth,
                surfaceBottom: 0f,
                showComposer,
                showCommandLine,
                commandLinePreview,
                composerText,
                showSlashPopup,
                slashRowCount,
                composerPreeditText,
                showSlashHierarchyHeader,
                composerFontSize,
                composerLineHeight,
                composerSlashPreview,
                composerSlashPreviewFontSize,
                commandLineFontSize,
                commandLinePreviewFontSize)
            .TotalHeight;

    public static void DrawDeckChrome(SKCanvas canvas, SKRect deckBounds, ISkiaKitPaintTheme theme)
    {
        if (deckBounds.Width <= 0f || deckBounds.Height <= 0f)
            return;

        using var fill = new SKPaint
        {
            Color = SkiaKitColor.Blend(theme.Surface, new SKColor(40, 48, 58), 0.06f),
            IsAntialias = true,
        };
        canvas.DrawRect(deckBounds, fill);

        using var topEdge = new SKPaint
        {
            Color = theme.Border,
            IsAntialias = true,
            StrokeWidth = 1f,
        };
        canvas.DrawLine(deckBounds.Left, deckBounds.Top + 0.5f, deckBounds.Right, deckBounds.Top + 0.5f, topEdge);
    }
}

#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Cockpit Command Line (TCI): RichTextKit caret/hit-test, selection, horizontal scroll. ADR 0138.</summary>
internal static class SkiaCommandLineStrip
{
    public const float HorizontalPadding = 10f;
    public const float VerticalPadding = 8f;
    public const string MonoFontFamily = "Cascadia Mono";

    private const float InputLineHeightPerPt = 22f / 12f;

    /// <summary>Высота строки ввода при 12pt (тесты, fallback).</summary>
    public static float InputLineHeightAt12Pt => InputLineHeightFor(12f);

    public static float InputLineHeightFor(float fontSize) => fontSize * InputLineHeightPerPt;

    public static SkiaTciTextField.Typography ResolveTypography(float fontSize) =>
        new(fontSize, InputLineHeightFor(fontSize), MonoFontFamily);

    public static float MinHeight(float fontSize = 12f) =>
        VerticalPadding * 2 + InputLineHeightFor(fontSize);

    public static float MeasureHeight(string? previewText = null, float fontSize = 12f, float previewFontSize = 10f) =>
        MinHeight(fontSize);

    public static SkiaTciTextField.Region ComputeInputRegion(SKRect bounds, float fontSize)
    {
        var lineHeight = InputLineHeightFor(fontSize);
        var textBounds = new SKRect(
            bounds.Left + HorizontalPadding,
            bounds.Top + VerticalPadding,
            bounds.Right - HorizontalPadding,
            bounds.Top + VerticalPadding + lineHeight);
        var inset = ComputeTextTopInset(fontSize, lineHeight, textBounds.Width);
        return new SkiaTciTextField.Region(textBounds, Math.Max(40f, textBounds.Width), inset);
    }

    public static float MaxHorizontalScroll(string bufferText, float contentWidth, float fontSize) =>
        SkiaTciTextField.MaxHorizontalScroll(
            bufferText,
            contentWidth,
            ResolveTypography(fontSize));

    public static bool TryHitTestCaretAtPoint(
        SKRect stripBounds,
        string bufferText,
        float pointX,
        float pointY,
        float fontSize,
        float scrollOffsetX,
        out int caretIndex)
    {
        caretIndex = 0;
        var region = ComputeInputRegion(stripBounds, fontSize);
        var typo = ResolveTypography(fontSize);
        return SkiaTciTextField.TryHitTestCaret(
            region,
            bufferText,
            typo,
            pointX,
            pointY,
            scrollOffsetX,
            0f,
            maxLines: 1,
            out caretIndex);
    }

    public static bool TryGetCaretRect(
        SKRect stripBounds,
        string bufferText,
        int caretIndex,
        float fontSize,
        float scrollOffsetX,
        out SKRect caretRect)
    {
        caretRect = default;
        var region = ComputeInputRegion(stripBounds, fontSize);
        var typo = ResolveTypography(fontSize);
        var origin = new SKPoint(region.TextBounds.Left, region.TextBounds.Top + region.TextTopInset);
        if (!SkiaTciTextField.TryGetCaretRect(
                region,
                bufferText,
                caretIndex,
                typo,
                origin,
                scrollOffsetX,
                0f,
                out caretRect))
            return false;

        if (caretRect.Height <= 0f)
        {
            caretRect = new SKRect(
                caretRect.Left,
                region.TextBounds.Top + 2f,
                caretRect.Right,
                region.TextBounds.Bottom - 2f);
        }

        return true;
    }

    public static void Draw(
        SKCanvas canvas,
        SKRect bounds,
        ISkiaKitPaintTheme theme,
        string bufferText,
        string? previewText,
        SlashCommandPreviewKind previewKind,
        string placeholder,
        bool isEnabled,
        int caretIndex,
        int selectionAnchor,
        bool showCaret,
        bool caretVisible,
        float scrollOffsetX,
        float fontSize = 12f,
        float previewFontSize = 10f)
    {
        using var fill = new SKPaint { Color = SkiaKitColor.Blend(theme.Surface, new SKColor(90, 140, 220), 0.08f), IsAntialias = true };
        canvas.DrawRect(bounds, fill);
        using var border = new SKPaint
        {
            Color = new SKColor(100, 160, 230, 180),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };
        canvas.DrawLine(bounds.Left, bounds.Top + 0.5f, bounds.Right, bounds.Top + 0.5f, border);

        drawInputLine(
            canvas,
            theme,
            ComputeInputRegion(bounds, fontSize),
            bufferText,
            placeholder,
            isEnabled,
            caretIndex,
            selectionAnchor,
            showCaret,
            caretVisible,
            scrollOffsetX,
            fontSize,
            previewKind);
    }

    private static float ComputeTextTopInset(float fontSize, float lineHeight, float contentWidth)
    {
        var bodyHeight = SkiaPlainTextLayout.MeasureBodyHeight(
            "Mg/",
            Math.Max(40f, contentWidth),
            fontSize,
            lineHeight,
            maxLines: 1,
            MonoFontFamily);
        return Math.Max(0f, (lineHeight - bodyHeight) * 0.5f);
    }

    private static void drawInputLine(
        SKCanvas canvas,
        ISkiaKitPaintTheme theme,
        SkiaTciTextField.Region region,
        string bufferText,
        string placeholder,
        bool isEnabled,
        int caretIndex,
        int selectionAnchor,
        bool showCaret,
        bool caretVisible,
        float scrollOffsetX,
        float fontSize,
        SlashCommandPreviewKind previewKind)
    {
        var typo = ResolveTypography(fontSize);
        var textBounds = region.TextBounds;
        var isEmpty = string.IsNullOrEmpty(bufferText);
        var display = isEmpty && !string.IsNullOrEmpty(placeholder) ? placeholder : bufferText;
        var contentColor = isEmpty && !string.IsNullOrEmpty(placeholder)
            ? theme.EmptyHint
            : isEnabled ? theme.Content : theme.EmptyHint;
        var linkColor = SkiaSlashPreviewChrome.ChipColors(theme, previewKind).Accent;

        var clipRect = textBounds;
        if (!isEmpty && SkiaSlashCommandChip.ShouldDraw(previewKind, bufferText))
            clipRect.Left -= SkiaStatusChip.IconLeadingOverhang;

        canvas.Save();
        canvas.ClipRect(clipRect);
        canvas.Translate(-scrollOffsetX, 0f);

        var textLeft = textBounds.Left + scrollOffsetX;
        var origin = new SKPoint(textLeft, textBounds.Top + region.TextTopInset);
        if (!isEmpty && SkiaSlashCommandChip.ShouldDraw(previewKind, bufferText))
        {
            var labelW = SkiaSlashCommandChip.MeasureLabelWidth(bufferText, fontSize);
            var chipRect = SkiaSlashCommandChip.ComputeChipRect(
                textLeft,
                textBounds.Top,
                typo.LineHeight,
                labelW);
            SkiaSlashCommandChip.Draw(canvas, chipRect, theme, previewKind, fontSize);
        }

        if (!isEmpty)
        {
            SkiaTciTextField.DrawSelection(
                canvas,
                bufferText,
                selectionAnchor,
                caretIndex,
                region,
                typo,
                origin,
                scrollOffsetX,
                0f,
                maxLines: 1);

            var layout = SkiaPlainTextLayout.TryMeasure(
                bufferText,
                region.ContentWidth,
                typo.FontSize,
                contentColor,
                maxLines: 1,
                typo.LineHeight,
                typo.FontFamily);
            if (layout is not null)
            {
                var textColor = SkiaSlashCommandChip.ShouldDraw(previewKind, bufferText) ? linkColor : contentColor;
                SkiaTciTextField.PaintBody(canvas, origin, layout, textColor, textColor);
            }
        }
        else if (!string.IsNullOrEmpty(placeholder))
        {
            using var font = SkiaKitFonts.CreateMono(fontSize);
            using var paint = new SKPaint { IsAntialias = true, Color = contentColor };
            canvas.DrawText(placeholder, origin.X, origin.Y, SKTextAlign.Left, font, paint);
        }

        canvas.Restore();

        if (!showCaret || !caretVisible || isEmpty)
            return;

        var caret = Math.Clamp(caretIndex, 0, bufferText.Length);
        var originCaret = new SKPoint(textBounds.Left, textBounds.Top + region.TextTopInset);
        if (SkiaTciTextField.TryGetCaretRect(
                region,
                bufferText,
                caret,
                typo,
                originCaret,
                scrollOffsetX,
                0f,
                out var caretRect))
            SkiaTciTextField.DrawCaretLine(canvas, caretRect.Left, caretRect.Top, caretRect.Bottom);
    }
}

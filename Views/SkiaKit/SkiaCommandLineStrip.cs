#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Полоса Cockpit Command Line над composer (ADR 0138 фаза A).</summary>
internal static class SkiaCommandLineStrip
{
    public const float HorizontalPadding = 10f;
    public const float InputLineHeight = 22f;
    public const float PreviewLineHeight = 14f;
    public const float VerticalPadding = 6f;
    public const float PreviewGap = 2f;

    public static float MinHeight => VerticalPadding * 2 + InputLineHeight;

    public static float MeasureHeight(string? previewText, float fontSize = 12f, float previewFontSize = 10f)
    {
        var h = MinHeight;
        if (!string.IsNullOrWhiteSpace(previewText))
            h += PreviewGap + PreviewLineHeight;
        _ = fontSize;
        _ = previewFontSize;
        return h;
    }

    public static void Draw(
        SKCanvas canvas,
        SKRect bounds,
        ISkiaKitPaintTheme theme,
        string bufferText,
        string? previewText,
        string placeholder,
        bool isEnabled,
        int caretIndex,
        bool showCaret,
        bool caretVisible,
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

        var inputTop = bounds.Top + VerticalPadding;
        var inputBottom = inputTop + InputLineHeight;
        drawInputLine(
            canvas,
            theme,
            new SKRect(bounds.Left + HorizontalPadding, inputTop, bounds.Right - HorizontalPadding, inputBottom),
            bufferText,
            placeholder,
            isEnabled,
            caretIndex,
            showCaret,
            caretVisible,
            fontSize);

        if (string.IsNullOrWhiteSpace(previewText))
            return;

        var previewTop = inputBottom + PreviewGap;
        using var previewFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), previewFontSize);
        using var previewPaint = new SKPaint { IsAntialias = true, Color = theme.EmptyHint };
        canvas.DrawText(
            previewText,
            bounds.Left + HorizontalPadding,
            previewTop + previewFontSize,
            SKTextAlign.Left,
            previewFont,
            previewPaint);
    }

    private static void drawInputLine(
        SKCanvas canvas,
        ISkiaKitPaintTheme theme,
        SKRect textBounds,
        string bufferText,
        string placeholder,
        bool isEnabled,
        int caretIndex,
        bool showCaret,
        bool caretVisible,
        float fontSize)
    {
        using var font = new SKFont(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal), fontSize);
        var display = string.IsNullOrEmpty(bufferText) && !string.IsNullOrEmpty(placeholder)
            ? placeholder
            : bufferText;
        var color = string.IsNullOrEmpty(bufferText) && !string.IsNullOrEmpty(placeholder)
            ? theme.EmptyHint
            : theme.Content;
        using var paint = new SKPaint { IsAntialias = true, Color = isEnabled ? color : theme.EmptyHint };
        var baseline = textBounds.MidY + fontSize * 0.35f;
        canvas.DrawText(display, textBounds.Left, baseline, SKTextAlign.Left, font, paint);

        if (!showCaret || !caretVisible || string.IsNullOrEmpty(bufferText))
            return;

        var caret = Math.Clamp(caretIndex, 0, bufferText.Length);
        var caretX = textBounds.Left + font.MeasureText(bufferText[..caret]);
        using var caretPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(120, 195, 255),
            StrokeWidth = 2f,
            Style = SKPaintStyle.Stroke,
        };
        canvas.DrawLine(caretX, textBounds.Top + 2f, caretX, textBounds.Bottom - 2f, caretPaint);
    }
}

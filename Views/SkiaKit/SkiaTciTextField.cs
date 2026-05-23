#nullable enable
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Topten.RichTextKit;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>
/// Общий Text-Command Interface (TCI): caret, hit-test, selection, horizontal scroll.
/// Используется composer, CCL, navigator search (ADR 0123, 0138).
/// </summary>
internal static class SkiaTciTextField
{
    public static readonly SKColor CaretColor = new(120, 195, 255);
    public static readonly SKColor SelectionFill = new(70, 130, 220, 90);
    private const float CaretStrokeWidth = 2f;

    public readonly record struct Typography(float FontSize, float LineHeight, string FontFamily);

    public readonly record struct Region(SKRect TextBounds, float ContentWidth, float TextTopInset);

    public static string MergeDisplay(string? text, string? preedit) =>
        string.IsNullOrEmpty(preedit) ? text ?? "" : (text ?? "") + preedit;

    public static float MeasureSingleLineWidth(string? text, Typography typo)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        return typo.FontFamily.Contains("Mono", StringComparison.OrdinalIgnoreCase)
            || typo.FontFamily.Contains("Cascadia", StringComparison.OrdinalIgnoreCase)
            || typo.FontFamily.Contains("Consolas", StringComparison.OrdinalIgnoreCase)
            ? SkiaKitFonts.CreateMono(typo.FontSize).MeasureText(text)
            : SkiaPlainTextLayout.MeasurePrefixWidth(text, typo.FontSize, typo.FontFamily);
    }

    public static float MaxHorizontalScroll(string? display, float contentWidth, Typography typo) =>
        Math.Max(0f, MeasureSingleLineWidth(display, typo) - contentWidth);

    public static bool TryHitTestCaret(
        Region region,
        string? display,
        Typography typo,
        float pointX,
        float pointY,
        float scrollOffsetX,
        float scrollOffsetY,
        int maxLines,
        out int caretIndex)
    {
        caretIndex = 0;
        if (!region.TextBounds.Contains(pointX, pointY))
            return false;

        var localX = pointX - region.TextBounds.Left + scrollOffsetX;
        var localY = pointY - region.TextBounds.Top - region.TextTopInset + scrollOffsetY;
        return SkiaPlainTextLayout.TryHitTestCaretIndex(
            display,
            localX,
            localY,
            region.ContentWidth,
            typo.FontSize,
            typo.LineHeight,
            out caretIndex,
            maxLines,
            typo.FontFamily);
    }

    public static bool TryGetCaretRect(
        Region region,
        string? display,
        int caretIndex,
        Typography typo,
        SKPoint textOrigin,
        float scrollOffsetX,
        float scrollOffsetY,
        out SKRect caretRect)
    {
        caretRect = default;
        var origin = new SKPoint(textOrigin.X - scrollOffsetX, textOrigin.Y - scrollOffsetY);
        if (!SkiaPlainTextLayout.TryGetCaretLine(
                display,
                caretIndex,
                region.ContentWidth,
                typo.FontSize,
                typo.LineHeight,
                origin,
                out var x,
                out var yTop,
                out var yBottom,
                int.MaxValue,
                typo.FontFamily))
            return false;

        caretRect = new SKRect(x, yTop, x + CaretStrokeWidth, yBottom);
        return true;
    }

    public static void DrawSelection(
        SKCanvas canvas,
        string display,
        int selectionAnchor,
        int caretIndex,
        Region region,
        Typography typo,
        SKPoint textOrigin,
        float scrollOffsetX,
        float scrollOffsetY,
        int maxLines)
    {
        var anchor = selectionAnchor;
        var start = Math.Min(anchor, caretIndex);
        var end = Math.Max(anchor, caretIndex);
        if (start >= end)
            return;

        var rs = SkiaPlainTextLayout.BuildRichString(
            display,
            region.ContentWidth,
            typo.FontSize,
            typo.LineHeight,
            maxLines,
            fontFamily: typo.FontFamily);

        var infoStart = rs.GetCaretInfo(new CaretPosition(start, altPosition: false));
        var infoEnd = rs.GetCaretInfo(new CaretPosition(end, altPosition: false));
        if (infoStart.IsNone || infoEnd.IsNone)
            return;

        using var paint = new SKPaint { Color = SelectionFill, IsAntialias = true, Style = SKPaintStyle.Fill };
        var line0 = Math.Min(infoStart.LineIndex, infoEnd.LineIndex);
        var line1 = Math.Max(infoStart.LineIndex, infoEnd.LineIndex);
        for (var line = line0; line <= line1; line++)
        {
            float left;
            float right;
            if (line == line0 && line == line1)
            {
                left = textOrigin.X + infoStart.CaretXCoord - scrollOffsetX;
                right = textOrigin.X + infoEnd.CaretXCoord - scrollOffsetX;
            }
            else if (line == line0)
            {
                left = textOrigin.X + infoStart.CaretXCoord - scrollOffsetX;
                right = textOrigin.X + region.ContentWidth - scrollOffsetX;
            }
            else if (line == line1)
            {
                left = textOrigin.X - scrollOffsetX;
                right = textOrigin.X + infoEnd.CaretXCoord - scrollOffsetX;
            }
            else
            {
                left = textOrigin.X - scrollOffsetX;
                right = textOrigin.X + region.ContentWidth - scrollOffsetX;
            }

            if (right < left)
                (left, right) = (right, left);

            var top = textOrigin.Y + line * typo.LineHeight - scrollOffsetY + 2f;
            var bottom = top + typo.LineHeight - 4f;
            canvas.DrawRect(SKRect.Create(left, top, right - left, bottom - top), paint);
        }
    }

    public static void DrawCaretLine(SKCanvas canvas, float x, float yTop, float yBottom)
    {
        using var caret = new SKPaint
        {
            Color = CaretColor,
            StrokeWidth = CaretStrokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        };
        canvas.DrawLine(x, yTop, x, yBottom, caret);
    }

    public static void PaintBody(
        SKCanvas canvas,
        SKPoint origin,
        SkiaRichTextKitBodyLayout layout,
        SKColor contentColor,
        SKColor codeColor) =>
        SkiaRichTextKitMarkdown.Paint(canvas, origin, layout, contentColor, codeColor);
}

#nullable enable
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Topten.RichTextKit;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Нижняя полоса ввода Intercom (рамка, текст, placeholder). ADR 0123 фаза 2.</summary>
internal static class SkiaComposerStrip
{
    public const float HorizontalPadding = 10f;
    public const float VerticalPadding = 8f;
    public const int MinLines = 3;
    public const float SendButtonWidth = 44f;
    public const float SendButtonHeight = 32f;
    public const float BorderWidth = 1f;

    private const float CaretVerticalPad = 2f;
    private static readonly SKColor CaretColor = new(120, 195, 255);
    private static readonly SKColor SelectionFill = new(70, 130, 220, 90);
    private const float CaretStrokeWidth = 2f;

    private readonly record struct TextLayout(SKRect TextBounds, float ContentWidth, float TextTopInset);

    public static float MinHeightFor(float lineHeight) =>
        VerticalPadding * 2 + MinLines * lineHeight;

    public static float MaxHeightFor(float lineHeight) =>
        VerticalPadding * 2 + 8 * lineHeight;

    public static float MeasureInnerContentHeight(
        string text,
        string? preeditText,
        float contentWidth,
        float fontSize,
        float lineHeight)
    {
        var display = BuildDisplayText(text, preeditText);
        return SkiaPlainTextLayout.MeasureBodyHeight(
            display,
            contentWidth,
            fontSize,
            lineHeight,
            maxLines: int.MaxValue);
    }

    public static float MaxContentScrollOffset(
        string text,
        string? preeditText,
        float contentWidth,
        float visibleInnerHeight,
        float fontSize,
        float lineHeight)
    {
        var inner = MeasureInnerContentHeight(text, preeditText, contentWidth, fontSize, lineHeight);
        return Math.Max(0f, inner - visibleInnerHeight);
    }

    public static float MeasureHeight(
        string text,
        string? preeditText,
        float contentWidth,
        float fontSize,
        float lineHeight)
    {
        var display = BuildDisplayText(text, preeditText);
        var minInner = MinLines * lineHeight;
        var maxLines = (int)((MaxHeightFor(lineHeight) - VerticalPadding * 2) / lineHeight);
        var rich = SkiaPlainTextLayout.TryMeasure(
            display,
            contentWidth,
            fontSize,
            new SKColor(220, 225, 235),
            Math.Max(MinLines, maxLines),
            lineHeight);
        var inner = Math.Max(minInner, rich?.BodyHeight ?? lineHeight);
        var minH = MinHeightFor(lineHeight);
        var maxH = MaxHeightFor(lineHeight);
        return Math.Clamp(inner + VerticalPadding * 2, minH, maxH);
    }

    public static bool TryHitTestCaretAtPoint(
        SKRect composerBounds,
        string text,
        string? preeditText,
        float pointX,
        float pointY,
        float fontSize,
        float lineHeight,
        float contentScrollOffsetY,
        out int caretIndex)
    {
        caretIndex = 0;
        if (composerBounds.Width <= 0)
            return false;

        var sendLeft = composerBounds.Right - HorizontalPadding - SendButtonWidth;
        var layout = ComputeTextLayout(composerBounds, sendLeft, fontSize, lineHeight);
        if (!layout.TextBounds.Contains(pointX, pointY))
            return false;

        var display = BuildDisplayText(text, preeditText);
        var maxLines = (int)((MaxHeightFor(lineHeight) - VerticalPadding * 2) / lineHeight);
        var localX = pointX - layout.TextBounds.Left;
        var localY = pointY - layout.TextBounds.Top - layout.TextTopInset + contentScrollOffsetY;
        return SkiaPlainTextLayout.TryHitTestCaretIndex(
            display,
            localX,
            localY,
            layout.ContentWidth,
            fontSize,
            lineHeight,
            out caretIndex,
            Math.Max(MinLines, maxLines));
    }

    public static void Draw(
        SKCanvas canvas,
        SKRect bounds,
        ISkiaKitPaintTheme theme,
        string text,
        string? preeditText,
        string placeholder,
        bool isEnabled,
        int caretIndex,
        bool showCaret,
        bool caretVisible,
        float fontSize,
        float lineHeight,
        out SKRect sendButtonBounds,
        out SKRect textBounds,
        int selectionAnchor = -1,
        float contentScrollOffsetY = 0f)
    {
        sendButtonBounds = default;
        textBounds = default;

        using var fill = new SKPaint { Color = theme.Surface, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRect(bounds, fill);

        using var border = new SKPaint
        {
            Color = theme.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = BorderWidth,
        };
        canvas.DrawLine(bounds.Left, bounds.Top + 0.5f, bounds.Right, bounds.Top + 0.5f, border);

        var sendLeft = bounds.Right - HorizontalPadding - SendButtonWidth;
        var sendTop = bounds.Top + (bounds.Height - SendButtonHeight) * 0.5f;
        sendButtonBounds = new SKRect(sendLeft, sendTop, sendLeft + SendButtonWidth, sendTop + SendButtonHeight);

        var layout = ComputeTextLayout(bounds, sendLeft, fontSize, lineHeight);
        textBounds = layout.TextBounds;
        var contentWidth = layout.ContentWidth;
        var textTopInset = layout.TextTopInset;
        var display = BuildDisplayText(text, preeditText);
        var isEmpty = string.IsNullOrEmpty(display);
        var textOrigin = new SKPoint(textBounds.Left, textBounds.Top + textTopInset);

        var selAnchor = selectionAnchor < 0 ? caretIndex : selectionAnchor;
        var maxLines = (int)((MaxHeightFor(lineHeight) - VerticalPadding * 2) / lineHeight);

        canvas.Save();
        canvas.ClipRect(textBounds);

        if (!isEmpty && selAnchor != caretIndex)
        {
            DrawSelectionHighlight(
                canvas,
                display,
                selAnchor,
                caretIndex,
                contentWidth,
                fontSize,
                lineHeight,
                textOrigin,
                contentScrollOffsetY,
                Math.Max(MinLines, maxLines));
        }

        canvas.Translate(0, -contentScrollOffsetY);

        if (isEmpty)
        {
            var hintLayout = SkiaPlainTextLayout.TryMeasure(
                placeholder,
                contentWidth,
                fontSize,
                theme.EmptyHint,
                MinLines,
                lineHeight);
            if (hintLayout is not null)
            {
                SkiaRichTextKitMarkdown.Paint(
                    canvas,
                    new SKPoint(textBounds.Left, textBounds.Top + textTopInset),
                    hintLayout,
                    theme.EmptyHint,
                    theme.EmptyHint);
            }
        }
        else
        {
            var rich = SkiaPlainTextLayout.TryMeasure(
                display,
                contentWidth,
                fontSize,
                isEnabled ? theme.Content : theme.EmptyHint,
                Math.Max(MinLines, maxLines),
                lineHeight);
            if (rich is not null)
            {
                SkiaRichTextKitMarkdown.Paint(
                    canvas,
                    textOrigin,
                    rich,
                    isEnabled ? theme.Content : theme.EmptyHint,
                    SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.35f));
            }
        }

        canvas.Restore();

        DrawSendButton(canvas, sendButtonBounds, theme, isEnabled, fontSize);

        if (isEnabled && showCaret && caretVisible && caretIndex >= 0
            && TryComputeCaretLine(
                layout,
                display,
                caretIndex,
                fontSize,
                lineHeight,
                textTopInset,
                textOrigin,
                contentScrollOffsetY,
                out var x,
                out var yTop,
                out var yBottom))
        {
            DrawCaretLine(canvas, x, yTop, yBottom);
        }
    }

    public static bool TryGetCaretRect(
        SKRect composerBounds,
        string text,
        string? preeditText,
        int caretIndex,
        float fontSize,
        float lineHeight,
        out SKRect caretRect,
        float contentScrollOffsetY = 0f)
    {
        caretRect = default;
        if (composerBounds.Width <= 0)
            return false;

        var sendLeft = composerBounds.Right - HorizontalPadding - SendButtonWidth;
        var layout = ComputeTextLayout(composerBounds, sendLeft, fontSize, lineHeight);
        var display = BuildDisplayText(text, preeditText);
        var textTopInset = lineHeight - 4f - fontSize * 0.85f - 4f;
        var textOrigin = new SKPoint(layout.TextBounds.Left, layout.TextBounds.Top + textTopInset);
        if (!TryComputeCaretLine(
                layout,
                display,
                caretIndex,
                fontSize,
                lineHeight,
                textTopInset,
                textOrigin,
                contentScrollOffsetY,
                out var x,
                out var yTop,
                out var yBottom))
            return false;

        caretRect = new SKRect(x, yTop, x + CaretStrokeWidth, yBottom);
        return true;
    }

    private static void DrawSelectionHighlight(
        SKCanvas canvas,
        string display,
        int anchor,
        int caret,
        float contentWidth,
        float fontSize,
        float lineHeight,
        SKPoint textOrigin,
        float scrollOffsetY,
        int maxLines)
    {
        var start = Math.Min(anchor, caret);
        var end = Math.Max(anchor, caret);
        if (start >= end)
            return;

        var rs = SkiaPlainTextLayout.BuildRichString(
            display,
            contentWidth,
            fontSize,
            lineHeight,
            maxLines);

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
                left = textOrigin.X + infoStart.CaretXCoord;
                right = textOrigin.X + infoEnd.CaretXCoord;
            }
            else if (line == line0)
            {
                left = textOrigin.X + infoStart.CaretXCoord;
                right = textOrigin.X + contentWidth;
            }
            else if (line == line1)
            {
                left = textOrigin.X;
                right = textOrigin.X + infoEnd.CaretXCoord;
            }
            else
            {
                left = textOrigin.X;
                right = textOrigin.X + contentWidth;
            }

            if (right < left)
                (left, right) = (right, left);

            var top = textOrigin.Y + line * lineHeight - scrollOffsetY + CaretVerticalPad;
            var bottom = top + lineHeight - CaretVerticalPad * 2f;
            canvas.DrawRect(SKRect.Create(left, top, right - left, bottom - top), paint);
        }
    }

    private static TextLayout ComputeTextLayout(SKRect bounds, float sendLeft, float fontSize, float lineHeight)
    {
        var textTopInset = lineHeight - 4f - fontSize * 0.85f - 4f;
        var textBounds = new SKRect(
            bounds.Left + HorizontalPadding,
            bounds.Top + VerticalPadding,
            sendLeft - 8f,
            bounds.Bottom - VerticalPadding);
        return new TextLayout(textBounds, Math.Max(40f, textBounds.Width), textTopInset);
    }

    private static bool TryComputeCaretLine(
        TextLayout layout,
        string display,
        int caretIndex,
        float fontSize,
        float lineHeight,
        float textTopInset,
        SKPoint textOrigin,
        float scrollOffsetY,
        out float x,
        out float yTop,
        out float yBottom)
    {
        var origin = new SKPoint(textOrigin.X, textOrigin.Y - scrollOffsetY);
        if (SkiaPlainTextLayout.TryGetCaretLine(
                display,
                caretIndex,
                layout.ContentWidth,
                fontSize,
                lineHeight,
                origin,
                out x,
                out yTop,
                out yBottom,
                int.MaxValue))
            return true;

        x = yTop = yBottom = 0f;
        return caretIndex >= 0;
    }

    private static void DrawCaretLine(SKCanvas canvas, float x, float yTop, float yBottom)
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

    private static void DrawSendButton(SKCanvas canvas, SKRect rect, ISkiaKitPaintTheme theme, bool enabled, float fontSize)
    {
        using var fill = new SKPaint
        {
            Color = enabled ? theme.HoverBorder : theme.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawRoundRect(rect, 6f, 6f, fill);

        var sendLabelPt = Math.Max(12f, fontSize + 2f);
        using var font = SkiaKitFonts.CreateUi(sendLabelPt, bold: true);
        using var paint = SkiaKitFonts.CreateTextPaint(SKColors.White);
        canvas.DrawText("↑", rect.MidX, rect.MidY + sendLabelPt * 0.35f, SKTextAlign.Center, font, paint);
    }

    private static string BuildDisplayText(string text, string? preeditText)
    {
        if (string.IsNullOrEmpty(preeditText))
            return text ?? "";
        return (text ?? "") + preeditText;
    }
}

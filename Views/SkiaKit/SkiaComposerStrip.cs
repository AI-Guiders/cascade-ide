#nullable enable
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;

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
    private const float CaretStrokeWidth = 2f;

    private readonly record struct TextLayout(SKRect TextBounds, float ContentWidth);

    public static float MinHeightFor(float lineHeight) =>
        VerticalPadding * 2 + MinLines * lineHeight;

    public static float MaxHeightFor(float lineHeight) =>
        VerticalPadding * 2 + 8 * lineHeight;

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
        var rich = SkiaRichTextKitMarkdown.TryMeasurePlain(
            display,
            contentWidth,
            fontSize: fontSize,
            color: new SKColor(220, 225, 235),
            maxLines: Math.Max(MinLines, maxLines),
            lineHeight: lineHeight);
        var inner = Math.Max(minInner, rich?.BodyHeight ?? lineHeight);
        var minH = MinHeightFor(lineHeight);
        var maxH = MaxHeightFor(lineHeight);
        return Math.Clamp(inner + VerticalPadding * 2, minH, maxH);
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
        out SKRect textBounds)
    {
        sendButtonBounds = default;
        textBounds = default;

        var textTopInset = lineHeight - 4f - fontSize * 0.85f - 4f;

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

        var layout = ComputeTextLayout(bounds, sendLeft);
        textBounds = layout.TextBounds;
        var contentWidth = layout.ContentWidth;
        var display = BuildDisplayText(text, preeditText);
        var isEmpty = string.IsNullOrEmpty(display);

        using var bodyFont = SkiaKitFonts.CreateUi(fontSize);

        if (isEmpty)
        {
            var hintLayout = SkiaRichTextKitMarkdown.TryMeasurePlain(
                placeholder,
                contentWidth,
                fontSize,
                theme.EmptyHint,
                maxLines: MinLines,
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
            var maxLines = (int)((MaxHeightFor(lineHeight) - VerticalPadding * 2) / lineHeight);
            var rich = SkiaRichTextKitMarkdown.TryMeasurePlain(
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
                    new SKPoint(textBounds.Left, textBounds.Top + textTopInset),
                    rich,
                    isEnabled ? theme.Content : theme.EmptyHint,
                    SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.35f));
            }
        }

        DrawSendButton(canvas, sendButtonBounds, theme, isEnabled, fontSize);

        if (isEnabled && showCaret && caretVisible && caretIndex >= 0
            && TryComputeCaretLine(layout, display, caretIndex, bodyFont, lineHeight, out var x, out var yTop, out var yBottom))
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
        out SKRect caretRect)
    {
        caretRect = default;
        if (composerBounds.Width <= 0)
            return false;

        var sendLeft = composerBounds.Right - HorizontalPadding - SendButtonWidth;
        var layout = ComputeTextLayout(composerBounds, sendLeft);
        var display = BuildDisplayText(text, preeditText);
        using var font = SkiaKitFonts.CreateUi(fontSize);
        if (!TryComputeCaretLine(layout, display, caretIndex, font, lineHeight, out var x, out var yTop, out var yBottom))
            return false;

        caretRect = new SKRect(x, yTop, x + CaretStrokeWidth, yBottom);
        return true;
    }

    private static TextLayout ComputeTextLayout(SKRect bounds, float sendLeft)
    {
        var textBounds = new SKRect(
            bounds.Left + HorizontalPadding,
            bounds.Top + VerticalPadding,
            sendLeft - 8f,
            bounds.Bottom - VerticalPadding);
        return new TextLayout(textBounds, Math.Max(40f, textBounds.Width));
    }

    private static bool TryComputeCaretLine(
        TextLayout layout,
        string display,
        int caretIndex,
        SKFont font,
        float lineHeight,
        out float x,
        out float yTop,
        out float yBottom)
    {
        x = yTop = yBottom = 0f;
        if (caretIndex < 0)
            return false;

        caretIndex = Math.Clamp(caretIndex, 0, display.Length);
        var before = display[..caretIndex];
        var beforeLines = WrapLines(before, layout.ContentWidth, font.Size);
        var lineIndex = Math.Max(0, beforeLines.Count - 1);
        var colText = beforeLines.Count == 0 ? "" : beforeLines[^1];
        x = layout.TextBounds.Left + font.MeasureText(colText);
        yTop = layout.TextBounds.Top + lineIndex * lineHeight + CaretVerticalPad;
        yBottom = yTop + lineHeight - CaretVerticalPad * 2f;
        return true;
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

    private static List<string> WrapLines(string text, float maxWidth, float fontSize)
    {
        using var font = SkiaKitFonts.CreateUi(fontSize);
        var lines = new List<string>();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            if (string.IsNullOrEmpty(raw))
            {
                lines.Add("");
                continue;
            }

            var current = "";
            foreach (var word in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(candidate) <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                    lines.Add(current);
                current = font.MeasureText(word) <= maxWidth ? word : BreakLong(word, font, maxWidth, lines);
            }

            if (current.Length > 0)
                lines.Add(current);
        }

        return lines.Count == 0 ? [""] : lines;
    }

    private static string BreakLong(string word, SKFont font, float maxWidth, List<string> lines)
    {
        var chunk = "";
        foreach (var ch in word)
        {
            var next = chunk + ch;
            if (font.MeasureText(next) > maxWidth && chunk.Length > 0)
            {
                lines.Add(chunk);
                chunk = ch.ToString();
            }
            else
                chunk = next;
        }

        return chunk;
    }
}

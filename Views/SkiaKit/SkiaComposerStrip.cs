#nullable enable
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Нижняя полоса ввода Intercom (рамка, текст, кнопка отправки). ADR 0123 фаза 2.</summary>
internal static class SkiaComposerStrip
{
    public const float MinHeight = 44f;
    public const float MaxHeight = 120f;
    public const float HorizontalPadding = 10f;
    public const float VerticalPadding = 8f;
    public const float SendButtonWidth = 44f;
    public const float SendButtonHeight = 32f;
    public const float BorderWidth = 1f;
    public const float LineHeight = 17f;

    public static float MeasureHeight(string text, string? preeditText, float contentWidth)
    {
        var display = BuildDisplayText(text, preeditText);
        var maxLines = (int)((MaxHeight - VerticalPadding * 2) / LineHeight);
        var rich = SkiaRichTextKitMarkdown.TryMeasurePlain(
            display,
            contentWidth,
            fontSize: 12f,
            color: new SKColor(220, 225, 235),
            maxLines: Math.Max(1, maxLines),
            lineHeight: LineHeight);
        var inner = rich?.BodyHeight ?? LineHeight;
        return Math.Clamp(inner + VerticalPadding * 2, MinHeight, MaxHeight);
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
        float layoutScale,
        out SKRect sendButtonBounds,
        out SKRect textBounds)
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

        textBounds = new SKRect(
            bounds.Left + HorizontalPadding,
            bounds.Top + VerticalPadding,
            sendLeft - 8f,
            bounds.Bottom - VerticalPadding);

        var contentWidth = Math.Max(40f, textBounds.Width);
        var display = BuildDisplayText(text, preeditText);
        var isEmpty = string.IsNullOrEmpty(display);

        using var bodyFont = SkiaKitFonts.CreateUi(12);

        if (isEmpty)
        {
            var hintLayout = SkiaRichTextKitMarkdown.TryMeasurePlain(
                placeholder,
                contentWidth,
                12f,
                theme.EmptyHint,
                maxLines: 4,
                LineHeight);
            if (hintLayout is not null)
            {
                SkiaRichTextKitMarkdown.Paint(
                    canvas,
                    new SKPoint(textBounds.Left, textBounds.Top + LineHeight - 4f - 12f * 0.85f),
                    hintLayout,
                    theme.EmptyHint,
                    theme.EmptyHint);
            }
        }
        else
        {
            var maxLines = (int)((MaxHeight - VerticalPadding * 2) / LineHeight);
            var rich = SkiaRichTextKitMarkdown.TryMeasurePlain(
                display,
                contentWidth,
                12f,
                isEnabled ? theme.Content : theme.EmptyHint,
                Math.Max(1, maxLines),
                LineHeight);
            if (rich is not null)
            {
                SkiaRichTextKitMarkdown.Paint(
                    canvas,
                    new SKPoint(textBounds.Left, textBounds.Top + LineHeight - 4f - 12f * 0.85f),
                    rich,
                    isEnabled ? theme.Content : theme.EmptyHint,
                    SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.35f));
            }
        }

        DrawSendButton(canvas, sendButtonBounds, theme, isEnabled);

        if (isEnabled && caretIndex >= 0 && !isEmpty)
            DrawCaret(canvas, textBounds, bodyFont, display, caretIndex, contentWidth);
    }

    private static void DrawSendButton(SKCanvas canvas, SKRect rect, ISkiaKitPaintTheme theme, bool enabled)
    {
        using var fill = new SKPaint
        {
            Color = enabled ? theme.HoverBorder : theme.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawRoundRect(rect, 6f, 6f, fill);

        using var font = SkiaKitFonts.CreateUi(14, bold: true);
        using var paint = SkiaKitFonts.CreateTextPaint(SKColors.White);
        canvas.DrawText("↑", rect.MidX, rect.MidY + 5f, SKTextAlign.Center, font, paint);
    }

    private static void DrawCaret(
        SKCanvas canvas,
        SKRect textBounds,
        SKFont font,
        string display,
        int caretIndex,
        float contentWidth)
    {
        caretIndex = Math.Clamp(caretIndex, 0, display.Length);
        var before = display[..caretIndex];
        var beforeLines = WrapLines(before, contentWidth);
        var lineIndex = Math.Max(0, beforeLines.Count - 1);
        var colText = beforeLines.Count == 0 ? "" : beforeLines[^1];

        var x = textBounds.Left + font.MeasureText(colText);
        var yTop = textBounds.Top + lineIndex * LineHeight;
        var yBottom = yTop + LineHeight;
        using var caret = new SKPaint { Color = new SKColor(220, 230, 245), StrokeWidth = 1.5f, IsAntialias = true };
        canvas.DrawLine(x, yTop + 2f, x, yBottom - 4f, caret);
    }

    private static string BuildDisplayText(string text, string? preeditText)
    {
        if (string.IsNullOrEmpty(preeditText))
            return text ?? "";
        return (text ?? "") + preeditText;
    }

    private static List<string> WrapLines(string text, float maxWidth)
    {
        using var font = SkiaKitFonts.CreateUi(12);
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

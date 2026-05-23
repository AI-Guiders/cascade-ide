#nullable enable
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Topten.RichTextKit;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Единая вёрстка plain-текста (RichTextKit): measure, caret, hit-test. ADR 0123.</summary>
internal static class SkiaPlainTextLayout
{
    public static string NormalizeBody(string? text) => (text ?? "").Replace("\r", "");

    public static RichString BuildRichString(
        string? text,
        float maxWidth,
        float fontSize,
        float lineHeight,
        int maxLines,
        SKColor? textColor = null,
        string fontFamily = "Segoe UI")
    {
        var body = NormalizeBody(text);
        var rs = new RichString { MaxWidth = maxWidth };
        rs.FontFamily(fontFamily).FontSize(fontSize);
        if (textColor.HasValue)
            rs.TextColor(textColor.Value);
        if (body.Length > 0)
            rs.Add(body);
        if (maxLines > 0 && maxLines < int.MaxValue)
            rs.MaxHeight = maxLines * lineHeight;
        return rs;
    }

    public static SkiaRichTextKitBodyLayout? TryMeasure(
        string? text,
        float maxWidth,
        float fontSize,
        SKColor color,
        int maxLines,
        float lineHeight,
        string fontFamily = "Segoe UI")
    {
        if (maxWidth < 8f)
            return null;

        var rs = BuildRichString(text, maxWidth, fontSize, lineHeight, maxLines, color, fontFamily);
        var height = rs.MeasuredHeight;
        if (height <= 0f)
            height = string.IsNullOrEmpty(text) ? 0f : lineHeight;

        return new SkiaRichTextKitBodyLayout
        {
            Body = text ?? "",
            MaxWidth = maxWidth,
            FontSize = fontSize,
            MaxBodyLines = maxLines,
            LineHeight = lineHeight,
            BodyHeight = height,
            FontFamily = fontFamily,
        };
    }

    public static float MeasureBodyHeight(
        string? text,
        float maxWidth,
        float fontSize,
        float lineHeight,
        int maxLines = int.MaxValue,
        string fontFamily = "Segoe UI")
    {
        var layout = TryMeasure(
            text,
            maxWidth,
            fontSize,
            new SKColor(220, 225, 235),
            maxLines,
            lineHeight,
            fontFamily);
        return layout?.BodyHeight ?? lineHeight;
    }

    public static bool TryGetCaretLine(
        string? text,
        int caretIndex,
        float maxWidth,
        float fontSize,
        float lineHeight,
        SKPoint origin,
        out float x,
        out float yTop,
        out float yBottom,
        int maxLines = int.MaxValue,
        string fontFamily = "Segoe UI")
    {
        x = yTop = yBottom = 0f;
        if (maxWidth < 8f || caretIndex < 0)
            return false;

        var body = NormalizeBody(text);
        caretIndex = Math.Clamp(caretIndex, 0, body.Length);

        var rs = BuildRichString(body, maxWidth, fontSize, lineHeight, maxLines, fontFamily: fontFamily);
        var caretInfo = rs.GetCaretInfo(new CaretPosition(caretIndex, altPosition: false));
        if (caretInfo.IsNone)
            return false;

        var caretRect = caretInfo.CaretRectangle;
        x = origin.X + caretInfo.CaretXCoord;
        yTop = origin.Y + caretRect.Top + CaretVerticalPad;
        yBottom = origin.Y + caretRect.Bottom - CaretVerticalPad;
        if (yBottom <= yTop)
            yBottom = yTop + Math.Max(lineHeight - CaretVerticalPad * 2f, 8f);
        return true;
    }

    public static bool TryHitTestCaretIndex(
        string? text,
        float localX,
        float localY,
        float maxWidth,
        float fontSize,
        float lineHeight,
        out int caretIndex,
        int maxLines = int.MaxValue,
        string fontFamily = "Segoe UI")
    {
        caretIndex = 0;
        if (maxWidth < 8f)
            return false;

        var body = NormalizeBody(text);
        var rs = BuildRichString(body, maxWidth, fontSize, lineHeight, maxLines, fontFamily: fontFamily);
        var hit = rs.HitTest(localX, localY);
        if (hit.IsNone)
            return false;

        caretIndex = Math.Clamp(hit.CaretPosition.CodePointIndex, 0, body.Length);
        return true;
    }

    /// <summary>Однострочный caret X (navigator search, CCL).</summary>
    public static float MeasurePrefixWidth(string prefix, float fontSize, string fontFamily = "Segoe UI")
    {
        if (prefix.Length == 0)
            return 0f;

        using var font = SkiaKitFonts.CreateUi(fontSize);
        return font.MeasureText(prefix);
    }

    private const float CaretVerticalPad = 2f;
}

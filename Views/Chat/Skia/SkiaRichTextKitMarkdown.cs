#nullable enable

using SkiaSharp;
using Topten.RichTextKit;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>RichTextKit layout/paint for inline markdown subset (same parser as <see cref="SkiaMarkdownLayout"/>).</summary>
internal sealed class SkiaRichTextKitBodyLayout
{
    public required string Body { get; init; }
    public float MaxWidth { get; init; }
    public float FontSize { get; init; }
    public int MaxBodyLines { get; init; }
    public float LineHeight { get; init; }
    public float BodyHeight { get; init; }
}

internal static class SkiaRichTextKitMarkdown
{
    public static SkiaRichTextKitBodyLayout? TryMeasure(
        string body,
        float maxWidth,
        float fontSize,
        SKColor contentColor,
        SKColor codeColor,
        int maxBodyLines,
        float lineHeight)
    {
        var rs = TryBuildRichString(body, maxWidth, fontSize, contentColor, codeColor, maxBodyLines, lineHeight);
        if (rs is null)
            return null;

        var height = rs.MeasuredHeight;
        if (height <= 0f)
            height = lineHeight;

        return new SkiaRichTextKitBodyLayout
        {
            Body = body,
            MaxWidth = maxWidth,
            FontSize = fontSize,
            MaxBodyLines = maxBodyLines,
            LineHeight = lineHeight,
            BodyHeight = height,
        };
    }

    public static void Paint(
        SKCanvas canvas,
        SKPoint origin,
        SkiaRichTextKitBodyLayout layout,
        SKColor contentColor,
        SKColor codeColor)
    {
        var rs = TryBuildRichString(
            layout.Body,
            layout.MaxWidth,
            layout.FontSize,
            contentColor,
            codeColor,
            layout.MaxBodyLines,
            layout.LineHeight);
        rs?.Paint(canvas, origin);
    }

    private static RichString? TryBuildRichString(
        string body,
        float maxWidth,
        float fontSize,
        SKColor contentColor,
        SKColor codeColor,
        int maxBodyLines,
        float lineHeight)
    {
        if (maxWidth < 8f || string.IsNullOrEmpty(body))
            return null;

        var runs = SkiaMarkdownLayout.ParseInline(body);
        if (runs.Count == 0)
            return null;

        var rs = new RichString
        {
            MaxWidth = maxWidth,
        };
        rs.FontFamily("Segoe UI").FontSize(fontSize).TextColor(contentColor);

        foreach (var run in runs)
        {
            if (run.Text.Length == 0)
                continue;

            switch (run.Style)
            {
                case SkiaMarkdownStyle.Bold:
                    rs.Add(run.Text, fontWeight: 700);
                    break;
                case SkiaMarkdownStyle.Italic:
                    rs.Add(run.Text, fontItalic: true);
                    break;
                case SkiaMarkdownStyle.Code:
                    rs.FontFamily("Cascadia Mono")
                        .TextColor(codeColor)
                        .Add(run.Text);
                    rs.FontFamily("Segoe UI").TextColor(contentColor).FontSize(fontSize);
                    break;
                default:
                    rs.Add(run.Text);
                    break;
            }
        }

        if (maxBodyLines > 0 && maxBodyLines < int.MaxValue)
            rs.MaxHeight = maxBodyLines * lineHeight;

        return rs;
    }
}

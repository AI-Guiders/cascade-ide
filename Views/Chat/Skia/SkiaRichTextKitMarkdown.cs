#nullable enable

using CascadeIDE.Views.SkiaKit;
using SkiaSharp;
using Topten.RichTextKit;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>RichTextKit layout/paint for inline and block markdown subsets.</summary>
internal sealed class SkiaRichTextKitBodyLayout
{
    public required string Body { get; init; }
    public float MaxWidth { get; init; }
    public float FontSize { get; init; }
    public int MaxBodyLines { get; init; }
    public float LineHeight { get; init; }
    public float BodyHeight { get; init; }
    public bool IsDocument { get; init; }
    public bool Compact { get; init; }
    public string FontFamily { get; init; } = "Segoe UI";
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
        float lineHeight,
        string fontFamily = "Segoe UI")
    {
        var rs = TryBuildRichString(body, maxWidth, fontSize, contentColor, codeColor, maxBodyLines, lineHeight, fontFamily);
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
            FontFamily = fontFamily,
        };
    }

    public static SkiaRichTextKitBodyLayout? TryMeasureDocument(
        string body,
        float maxWidth,
        float baseFontSize,
        SKColor contentColor,
        SKColor codeColor,
        int maxRows,
        float lineHeight,
        bool compact)
    {
        if (maxWidth < 8f || string.IsNullOrWhiteSpace(body))
            return null;

        var maxChars = Math.Max(8, (int)(maxWidth / 6.5f));
        var rows = SkiaMarkdownDocument.Layout(body, maxChars);
        var rs = BuildRichStringFromDocument(rows, maxWidth, baseFontSize, contentColor, codeColor, compact);
        if (rs is null)
            return null;

        if (maxRows > 0 && maxRows < int.MaxValue)
            rs.MaxHeight = maxRows * lineHeight;

        var height = rs.MeasuredHeight;
        if (height <= 0f)
            height = lineHeight;

        return new SkiaRichTextKitBodyLayout
        {
            Body = body,
            MaxWidth = maxWidth,
            FontSize = baseFontSize,
            MaxBodyLines = maxRows,
            LineHeight = lineHeight,
            BodyHeight = height,
            IsDocument = true,
            Compact = compact,
        };
    }

    public static SkiaRichTextKitBodyLayout? TryMeasurePlain(
        string text,
        float maxWidth,
        float fontSize,
        SKColor color,
        int maxLines,
        float lineHeight,
        string fontFamily = "Segoe UI")
    {
        if (maxWidth < 8f)
            return null;

        var rs = new RichString { MaxWidth = maxWidth };
        rs.FontFamily(fontFamily).FontSize(fontSize).TextColor(color);
        if (!string.IsNullOrEmpty(text))
            rs.Add(text.Replace("\r", ""));

        if (maxLines > 0 && maxLines < int.MaxValue)
            rs.MaxHeight = maxLines * lineHeight;

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

    public static void Paint(SKCanvas canvas, SKPoint origin, SkiaRichTextKitBodyLayout layout, SKColor contentColor, SKColor codeColor)
    {
        RichString? rs;
        if (layout.IsDocument)
        {
            var maxChars = Math.Max(8, (int)(layout.MaxWidth / 6.5f));
            var rows = SkiaMarkdownDocument.Layout(layout.Body, maxChars);
            rs = BuildRichStringFromDocument(rows, layout.MaxWidth, layout.FontSize, contentColor, codeColor, layout.Compact);
            if (rs is not null && layout.MaxBodyLines > 0 && layout.MaxBodyLines < int.MaxValue)
                rs.MaxHeight = layout.MaxBodyLines * layout.LineHeight;
        }
        else
        {
            rs = TryBuildRichString(
                layout.Body,
                layout.MaxWidth,
                layout.FontSize,
                contentColor,
                codeColor,
                layout.MaxBodyLines,
                layout.LineHeight,
                layout.FontFamily);
        }

        rs?.Paint(canvas, origin);
    }

    private static RichString? TryBuildRichString(
        string body,
        float maxWidth,
        float fontSize,
        SKColor contentColor,
        SKColor codeColor,
        int maxBodyLines,
        float lineHeight,
        string fontFamily)
    {
        if (maxWidth < 8f || string.IsNullOrEmpty(body))
            return null;

        var runs = SkiaMarkdownLayout.ParseInline(body);
        if (runs.Count == 0)
            return null;

        var rs = new RichString { MaxWidth = maxWidth };
        rs.FontFamily(fontFamily).FontSize(fontSize).TextColor(contentColor);

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
                    rs.FontFamily(fontFamily).TextColor(contentColor).FontSize(fontSize);
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

    private static RichString? BuildRichStringFromDocument(
        IReadOnlyList<SkiaMarkdownRow> rows,
        float maxWidth,
        float baseFontSize,
        SKColor contentColor,
        SKColor codeColor,
        bool compact)
    {
        if (maxWidth < 8f || rows.Count == 0)
            return null;

        var rs = new RichString { MaxWidth = maxWidth };
        var first = true;
        foreach (var row in rows)
        {
            if (!first && row.Kind != SkiaMarkdownBlockKind.Blank)
                rs.Add("\n");
            first = false;

            switch (row.Kind)
            {
                case SkiaMarkdownBlockKind.Blank:
                    rs.Add("\n");
                    break;
                case SkiaMarkdownBlockKind.HorizontalRule:
                    rs.FontFamily("Segoe UI")
                        .FontSize(baseFontSize)
                        .TextColor(SkiaKitColor.Blend(contentColor, codeColor, 0.5f))
                        .Add(compact ? "—" : "────────");
                    break;
                case SkiaMarkdownBlockKind.Heading1:
                    AppendInlineRuns(rs, row.Runs, baseFontSize * 1.28f, contentColor, codeColor, boldDefault: true);
                    break;
                case SkiaMarkdownBlockKind.Heading2:
                    AppendInlineRuns(rs, row.Runs, baseFontSize * 1.14f, contentColor, codeColor, boldDefault: true);
                    break;
                case SkiaMarkdownBlockKind.Heading3:
                    AppendInlineRuns(rs, row.Runs, baseFontSize * 1.06f, contentColor, codeColor, boldDefault: true);
                    break;
                case SkiaMarkdownBlockKind.Bullet:
                case SkiaMarkdownBlockKind.Paragraph:
                default:
                    AppendInlineRuns(rs, row.Runs, baseFontSize, contentColor, codeColor, boldDefault: false);
                    break;
            }
        }

        return rs;
    }

    private static void AppendInlineRuns(
        RichString rs,
        IReadOnlyList<SkiaMarkdownRun> runs,
        float fontSize,
        SKColor contentColor,
        SKColor codeColor,
        bool boldDefault)
    {
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
                    rs.FontFamily("Cascadia Mono").TextColor(codeColor).Add(run.Text);
                    rs.FontFamily("Segoe UI").TextColor(contentColor).FontSize(fontSize);
                    break;
                default:
                    if (boldDefault)
                        rs.Add(run.Text, fontWeight: 700);
                    else
                        rs.Add(run.Text);
                    break;
            }
        }
    }
}

#nullable enable
using CascadeIDE.Views.Chat.Skia;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaMarkdownDocumentTests
{
    [Fact]
    public void Layout_maps_headings_lists_and_hr()
    {
        const string md = """
            # Title
            ## Section
            - one **bold**
            ---
            plain
            """;

        var rows = SkiaMarkdownDocument.Layout(md, maxChars: 80);
        Assert.Contains(rows, r => r.Kind == SkiaMarkdownBlockKind.Heading1);
        Assert.Contains(rows, r => r.Kind == SkiaMarkdownBlockKind.Heading2);
        Assert.Contains(rows, r => r.Kind == SkiaMarkdownBlockKind.Bullet);
        Assert.Contains(rows, r => r.Kind == SkiaMarkdownBlockKind.HorizontalRule);
        Assert.Contains(rows, r => r.Kind == SkiaMarkdownBlockKind.Paragraph);

        var bullet = rows.First(r => r.Kind == SkiaMarkdownBlockKind.Bullet);
        Assert.Equal(SkiaMarkdownStyle.Plain, bullet.Runs[0].Style);
        Assert.Contains(bullet.Runs, r => r.Style == SkiaMarkdownStyle.Bold && r.Text.Trim() == "bold");
    }

    [Fact]
    public void Layout_wraps_long_bullet_continuation()
    {
        var md = "- " + new string('x', 40);
        var rows = SkiaMarkdownDocument.Layout(md, maxChars: 12);
        Assert.True(rows.Count(r => r.Kind == SkiaMarkdownBlockKind.Bullet) >= 2);
        Assert.StartsWith("  ", rows[^1].Runs[0].Text, StringComparison.Ordinal);
    }
}

#nullable enable
using CascadeIDE.Views.Chat.Skia;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaMarkdownLayoutTests
{
    [Fact]
    public void ParseInline_parses_bold_italic_and_code()
    {
        var runs = SkiaMarkdownLayout.ParseInline("a **b** `c` *d*");
        Assert.Contains(runs, r => r.Style == SkiaMarkdownStyle.Bold && r.Text == "b");
        Assert.Contains(runs, r => r.Style == SkiaMarkdownStyle.Code && r.Text == "c");
        Assert.Contains(runs, r => r.Style == SkiaMarkdownStyle.Italic && r.Text == "d");
        Assert.Contains(runs, r => r.Style == SkiaMarkdownStyle.Plain && r.Text.Contains('a'));
    }

    [Fact]
    public void WrapLines_preserves_styles_across_words()
    {
        var runs = SkiaMarkdownLayout.ParseInline("**hello world**");
        var lines = SkiaMarkdownLayout.WrapLines(runs, maxChars: 8);
        Assert.NotEmpty(lines);
        Assert.Contains(lines[0].Runs, r => r.Style == SkiaMarkdownStyle.Bold);
    }

    [Fact]
    public void ToPlainText_strips_markers()
    {
        var runs = SkiaMarkdownLayout.ParseInline("**x**");
        var lines = SkiaMarkdownLayout.WrapLines(runs, 40);
        Assert.Equal("x", SkiaMarkdownLayout.ToPlainText(lines));
    }
}

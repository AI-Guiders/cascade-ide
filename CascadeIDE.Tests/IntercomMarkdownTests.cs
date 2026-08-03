#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomMarkdownTests
{
    [Fact]
    public void ParseInline_parses_bold_italic_and_code()
    {
        var runs = IntercomMarkdown.ParseInline("a **b** `c` *d*");
        Assert.Contains(runs, r => r.Style == IntercomMarkdownStyle.Bold && r.Text == "b");
        Assert.Contains(runs, r => r.Style == IntercomMarkdownStyle.Code && r.Text == "c");
        Assert.Contains(runs, r => r.Style == IntercomMarkdownStyle.Italic && r.Text == "d");
    }

    [Fact]
    public void SplitSegments_extracts_fenced_code()
    {
        var segs = IntercomMarkdown.SplitSegments("intro\n```\nvar x = 1;\n```\noutro");
        Assert.Equal(3, segs.Count);
        Assert.Equal(IntercomMarkdownSegmentKind.Prose, segs[0].Kind);
        Assert.Equal(IntercomMarkdownSegmentKind.Code, segs[1].Kind);
        Assert.Equal("var x = 1;", segs[1].Text);
        Assert.Equal(IntercomMarkdownSegmentKind.Prose, segs[2].Kind);
    }

    [Fact]
    public void LayoutDocument_headers_and_bullets()
    {
        var rows = IntercomMarkdown.LayoutDocument("# Title\n- item **one**\nplain", maxChars: 80);
        Assert.Contains(rows, r => r.Kind == IntercomMarkdownBlockKind.Heading1);
        Assert.Contains(rows, r => r.Kind == IntercomMarkdownBlockKind.Bullet);
        Assert.Contains(rows, r => r.Kind == IntercomMarkdownBlockKind.Paragraph);
        Assert.Contains(
            rows.SelectMany(r => r.Runs),
            run => run.Style == IntercomMarkdownStyle.Bold && run.Text.Contains("one", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseInline_snake_case_does_not_hang()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var runs = IntercomMarkdown.ParseInline("hello_world and Foo_Bar_Baz");
        sw.Stop();
        Assert.NotEmpty(runs);
        Assert.True(sw.ElapsedMilliseconds < 100, $"ParseInline hung/slow: {sw.ElapsedMilliseconds}ms");
    }
}

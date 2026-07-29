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

    [Fact]
    public void HasInlineMarkup_detects_markers()
    {
        Assert.False(SkiaMarkdownLayout.HasInlineMarkup("plain text only"));
        Assert.True(SkiaMarkdownLayout.HasInlineMarkup("has *star*"));
        Assert.True(SkiaMarkdownLayout.HasInlineMarkup("[F:Foo.cs]"));
    }

    [Fact]
    public void ParseInline_snake_case_underscore_does_not_hang()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var runs = SkiaMarkdownLayout.ParseInline("hello_world and Foo_Bar_Baz");
        sw.Stop();
        Assert.NotEmpty(runs);
        Assert.True(sw.ElapsedMilliseconds < 100, $"ParseInline hung/slow: {sw.ElapsedMilliseconds}ms");
        Assert.Contains(runs, r => r.Text.Contains('_', StringComparison.Ordinal));
    }

    [Fact]
    public void ParseInline_unmatched_markers_complete()
    {
        var runs = SkiaMarkdownLayout.ParseInline("a [ open * star ` tick");
        Assert.NotEmpty(runs);
        Assert.Equal("a [ open * star ` tick", string.Concat(runs.Select(r => r.Text)));
    }


    [Fact]
    public void Feed_measure_of_large_plain_body_stays_under_budget()
    {
        var body = new string('x', SkiaChatRenderLimits.MaxProseBodyChars * 4);
        var ctx = new SkiaChatMeasureContext(60, 480);
        var spec = new SkiaChatBubbleSpec(
            Title: "agent",
            Body: body,
            Footer: null,
            Kind: SkiaChatBubbleKind.Feed,
            FillRole: SkiaBubbleFillRole.MessageAssistant,
            BodyTone: SkiaChatBodyTone.Normal,
            IsPending: false,
            IsSelected: false,
            StartsBranch: false,
            MessageIndex: 1);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var metrics = SkiaChatBubbleRenderer.Measure(ctx, spec);
        sw.Stop();

        Assert.Null(metrics.RichTextBody);
        Assert.NotEmpty(metrics.ContentLines);
        Assert.True(sw.ElapsedMilliseconds < 750, $"measure took {sw.ElapsedMilliseconds}ms");
    }

}

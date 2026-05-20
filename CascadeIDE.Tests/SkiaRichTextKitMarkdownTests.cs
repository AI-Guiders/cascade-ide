using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaRichTextKitMarkdownTests
{
    [Fact]
    public void TryMeasure_inline_subset_returns_positive_height()
    {
        var layout = SkiaRichTextKitMarkdown.TryMeasure(
            "a **bold** `code` *italic*",
            maxWidth: 400f,
            fontSize: 11f,
            contentColor: SKColors.White,
            codeColor: SKColors.LightGray,
            maxBodyLines: int.MaxValue,
            lineHeight: 16f);

        Assert.NotNull(layout);
        Assert.True(layout.BodyHeight > 8f);
        Assert.Equal("a **bold** `code` *italic*", layout.Body);
    }

    [Fact]
    public void Feed_bubble_uses_rich_text_layout()
    {
        var ctx = new SkiaChatMeasureContext(60, 480);
        var spec = new SkiaChatBubbleSpec(
            Title: "agent",
            Body: "Hello **world**",
            Footer: null,
            Kind: SkiaChatBubbleKind.Feed,
            FillRole: SkiaBubbleFillRole.MessageAssistant,
            BodyTone: SkiaChatBodyTone.Normal,
            IsPending: false,
            IsSelected: false,
            StartsBranch: false,
            MessageIndex: 1);

        var metrics = SkiaChatBubbleRenderer.Measure(ctx, spec);
        Assert.NotNull(metrics.RichTextBody);
        Assert.True(SkiaChatBubbleRenderer.MeasureHeight(spec, metrics) > 20f);
    }

    [Fact]
    public void TryMeasureDocument_heading2_returns_positive_height()
    {
        var layout = SkiaRichTextKitMarkdown.TryMeasureDocument(
            "## Title\n\nBody **bold**",
            maxWidth: 400f,
            baseFontSize: 11f,
            contentColor: SKColors.White,
            codeColor: SKColors.LightGray,
            maxRows: 64,
            lineHeight: 15f,
            compact: false);

        Assert.NotNull(layout);
        Assert.True(layout.IsDocument);
        Assert.True(layout.BodyHeight > 15f);
    }
}

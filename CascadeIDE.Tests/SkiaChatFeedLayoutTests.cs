#nullable enable

using CascadeIDE.Views.Chat.Skia;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaChatFeedLayoutTests
{
    [Fact]
    public void Feed_measure_height_has_no_bubble_padding()
    {
        var ctx = new SkiaChatMeasureContext(60, 480);
        var body = "Line one\nLine two";
        var feedSpec = new SkiaChatBubbleSpec(
            "Агент",
            body,
            Footer: null,
            SkiaChatBubbleKind.Feed,
            SkiaBubbleFillRole.MessageAssistant,
            SkiaChatBodyTone.Normal,
            IsPending: false,
            IsSelected: false,
            StartsBranch: false,
            MessageIndex: 0,
            Padding: 0,
            TitleHeight: 16,
            LineHeight: 15);

        var bubbleSpec = feedSpec with { Kind = SkiaChatBubbleKind.Standard, Padding = 10 };

        var feedMetrics = SkiaChatBubbleRenderer.Measure(ctx, feedSpec);
        var bubbleMetrics = SkiaChatBubbleRenderer.Measure(ctx, bubbleSpec);

        var feedHeight = SkiaChatBubbleRenderer.MeasureHeight(feedSpec, feedMetrics);
        var bubbleHeight = SkiaChatBubbleRenderer.MeasureHeight(bubbleSpec, bubbleMetrics);

        Assert.True(feedHeight < bubbleHeight);
    }
}

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

    [Fact]
    public void Body_column_shifts_right_when_role_rail_shown()
    {
        var layout = SkiaChatFeedLayout.For(forwardHost: false);
        var without = layout.BodyColumn(contentLeft: 40f, contentWidth: 400f, includeRoleRail: false);
        var with = layout.BodyColumn(contentLeft: 40f, contentWidth: 400f, includeRoleRail: true);

        Assert.Equal(40f, without.Left);
        Assert.Equal(400f, without.Width);
        Assert.Equal(40f + layout.RoleRailWidth, with.Left);
        Assert.Equal(400f - layout.RoleRailWidth, with.Width);
    }

    [Fact]
    public void Slash_text_column_reserves_role_rail_and_icon()
    {
        var layout = SkiaChatFeedLayout.For(forwardHost: true);
        var column = layout.SlashTextColumn(contentLeft: 0f, contentWidth: 320f);

        Assert.True(column.Left > layout.RoleRailWidth);
        Assert.True(column.Width < 320f - layout.RoleRailWidth - layout.SlashIconReserve);
    }
}

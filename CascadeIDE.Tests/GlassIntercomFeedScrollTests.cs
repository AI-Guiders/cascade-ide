#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomFeedScrollTests
{
    [Fact]
    public void Content_fits_counts_as_pinned()
    {
        Assert.True(GlassIntercomFeedScroll.IsPinnedToEnd(0, 100, 200));
    }

    [Fact]
    public void Near_bottom_within_slack_is_pinned()
    {
        Assert.True(GlassIntercomFeedScroll.IsPinnedToEnd(376, 500, 100, slackPx: 24));
    }

    [Fact]
    public void Mid_history_is_not_pinned()
    {
        Assert.False(GlassIntercomFeedScroll.IsPinnedToEnd(50, 500, 100, slackPx: 24));
    }

    [Fact]
    public void Resolve_sticks_when_pinned()
    {
        Assert.Equal(double.PositiveInfinity,
            GlassIntercomFeedScroll.ResolveOffsetAfterRebuild(false, wasPinnedToEnd: true, priorOffset: 40));
    }

    [Fact]
    public void Resolve_keeps_offset_when_reading()
    {
        Assert.Equal(120,
            GlassIntercomFeedScroll.ResolveOffsetAfterRebuild(false, wasPinnedToEnd: false, priorOffset: 120));
    }

    [Fact]
    public void Resolve_stickEnd_wins()
    {
        Assert.Equal(double.PositiveInfinity,
            GlassIntercomFeedScroll.ResolveOffsetAfterRebuild(true, wasPinnedToEnd: false, priorOffset: 120));
    }
}

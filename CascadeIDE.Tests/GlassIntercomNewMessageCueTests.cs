#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomNewMessageCueTests
{
    [Fact]
    public void Arrival_while_reading_increments()
    {
        Assert.Equal(1, GlassIntercomNewMessageCue.AfterArrival(0, wasPinnedToEnd: false));
        Assert.Equal(3, GlassIntercomNewMessageCue.AfterArrival(2, wasPinnedToEnd: false));
    }

    [Fact]
    public void Arrival_while_pinned_clears()
    {
        Assert.Equal(0, GlassIntercomNewMessageCue.AfterArrival(5, wasPinnedToEnd: true));
    }

    [Fact]
    public void Format_singular_and_plural()
    {
        Assert.Equal("↓ new", GlassIntercomNewMessageCue.FormatLabel(1));
        Assert.Equal("↓ 4 new", GlassIntercomNewMessageCue.FormatLabel(4));
        Assert.Equal("", GlassIntercomNewMessageCue.FormatLabel(0));
    }

    [Fact]
    public void ShouldShow_requires_pending()
    {
        Assert.False(GlassIntercomNewMessageCue.ShouldShow(0));
        Assert.True(GlassIntercomNewMessageCue.ShouldShow(1));
    }
}

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.HybridIndex.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class HybridIndexHisPresentationProjectionTests
{
    [Fact]
    public void Lamp_empty_state_is_no_data() =>
        Assert.Equal("NO DATA", HybridIndexHisPresentationProjection.LampText(null));

    [Fact]
    public void State_empty_is_dash() =>
        Assert.Equal("—", HybridIndexHisPresentationProjection.StateShort(null));

    [Theory]
    [InlineData("", "OK", "IDLE")]
    [InlineData(" ", "OK", "IDLE")]
    [InlineData("oops", "CAUTION", "ERROR")]
    public void Lamp_and_state_follow_last_error(string lastError, string lamp, string state)
    {
        var evt = Make(lastError);
        Assert.Equal(lamp, HybridIndexHisPresentationProjection.LampText(evt));
        Assert.Equal(state, HybridIndexHisPresentationProjection.StateShort(evt));
    }

    private static HybridIndexStateChanged Make(string? lastError) =>
        new("/", null, ":memory:", 0, null, lastError, null);

    [Theory]
    [InlineData("", "NO FAILURES")]
    [InlineData("—", "NO FAILURES")]
    [InlineData("disk full", "disk full")]
    public void Second_line_for_failures_row(string banner, string expect) =>
        Assert.Equal(expect, HybridIndexHisPresentationProjection.SecondMessageLine(banner));
}

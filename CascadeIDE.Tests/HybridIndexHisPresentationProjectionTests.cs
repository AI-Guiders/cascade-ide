using System.Globalization;
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

    [Fact]
    public void Freshness_minutes_rounded_and_ecam_under_one_hour()
    {
        Assert.Equal("0", HybridIndexHisPresentationProjection.FreshnessMinutesRoundedText(0.2));
        Assert.Equal("12m", HybridIndexHisPresentationProjection.FreshnessEcamText(12.8));
    }

    [Fact]
    public void Freshness_colon_line_uses_wall_clock()
    {
        var iso = DateTimeOffset.Parse("2020-01-01T12:00:00Z").ToString("o", CultureInfo.InvariantCulture);
        var now = DateTimeOffset.Parse("2020-01-02T12:00:00Z");
        Assert.Contains("freshness:", HybridIndexHisPresentationProjection.FreshnessColonLine(iso, now));
        Assert.Contains("d", HybridIndexHisPresentationProjection.FreshnessColonLine(iso, now));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1500, 0.5)]
    [InlineData(3000, 1)]
    public void Docs_gauge_docs_count(int docs, double expect01)
    {
        Assert.Equal(expect01, HybridIndexHisPresentationProjection.DocsGauge01(docs));
    }
}

#nullable enable

using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftOrganChromeAggregatorTests
{
    [Fact]
    public void Empty_band_has_no_content()
    {
        var a = new SoftOrganChromeAggregator();
        var band = a.Snapshot();
        Assert.False(band.HasContent);
        Assert.False(band.HasOverflow);
        Assert.Empty(band.VisibleLines);
    }

    [Fact]
    public void Three_or_fewer_show_all_without_overflow()
    {
        var a = new SoftOrganChromeAggregator();
        a.Apply("plan", "plan latch");
        a.Apply("crm", "crm latch");
        a.Apply("learn", "learn latch");

        var band = a.Snapshot();
        Assert.Equal(3, band.VisibleLines.Count);
        Assert.Equal(0, band.HiddenCount);
        Assert.Null(band.OverflowLine);
        Assert.Equal("plan latch", band.VisibleLines[0]);
    }

    [Fact]
    public void Overflow_separates_from_visible_lines()
    {
        var a = new SoftOrganChromeAggregator();
        a.Apply("pressure", "p");
        a.Apply("ignite", "i");
        a.Apply("plan", "pl");
        a.Apply("crm", "c");
        a.Apply("learn", "l");
        a.Apply("webcam", "w");
        a.Apply("toolchain", "t");

        var band = a.Snapshot();
        Assert.Equal(3, band.VisibleLines.Count);
        Assert.Equal(4, band.HiddenCount);
        Assert.Equal("+4 more · SoftOrgan latches", band.OverflowLine);
        Assert.True(band.HasOverflow);
        Assert.DoesNotContain(band.VisibleLines, line => line.Contains("more", StringComparison.Ordinal));
    }

    [Fact]
    public void Clear_hint_removes_organ()
    {
        var a = new SoftOrganChromeAggregator();
        a.Apply("plan", "plan latch");
        a.Apply("plan", null);
        Assert.False(a.Snapshot().HasContent);
    }

    [Fact]
    public void Priority_orders_pressure_before_learn()
    {
        Assert.True(SoftOrganChromeAggregator.PriorityFor("pressure") < SoftOrganChromeAggregator.PriorityFor("learn"));
    }
}

#nullable enable

using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftInstrumentChromeAggregatorTests
{
    [Fact]
    public void Empty_band_has_no_content()
    {
        var a = new SoftInstrumentChromeAggregator();
        var band = a.Snapshot();
        Assert.False(band.HasContent);
        Assert.False(band.HasOverflow);
        Assert.Empty(band.VisibleLines);
    }

    [Fact]
    public void Three_or_fewer_show_all_without_overflow()
    {
        var a = new SoftInstrumentChromeAggregator();
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
        var a = new SoftInstrumentChromeAggregator();
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
        Assert.Equal("+4 more · SoftInstrument latches", band.OverflowLine);
        Assert.True(band.HasOverflow);
        Assert.DoesNotContain(band.VisibleLines, line => line.Contains("more", StringComparison.Ordinal));
    }

    [Fact]
    public void Clear_hint_removes_organ()
    {
        var a = new SoftInstrumentChromeAggregator();
        a.Apply("plan", "plan latch");
        a.Apply("plan", null);
        Assert.False(a.Snapshot().HasContent);
    }

    [Fact]
    public void Apply_ignores_unknown_and_eicas_ids()
    {
        var a = new SoftInstrumentChromeAggregator();
        a.Apply("not-an-organ", "noise");
        a.Apply("alert", "eicas bleed");
        a.Apply("qrh", "advisory bleed");
        Assert.False(a.Snapshot().HasContent);

        a.Apply("plan", "plan latch");
        a.Apply("unknown", "still noise");
        var band = a.Snapshot();
        Assert.True(band.HasContent);
        Assert.Single(band.VisibleLines);
        Assert.Equal("plan latch", band.VisibleLines[0]);
    }

    [Fact]
    public void Priority_orders_pressure_before_learn()
    {
        Assert.True(SoftInstrumentChromeAggregator.PriorityFor("pressure") < SoftInstrumentChromeAggregator.PriorityFor("learn"));
    }

    [Fact]
    public void Expand_then_collapse_round_trips()
    {
        var a = SeedOverflow();

        Assert.True(a.ToggleExpanded());
        var expanded = a.Snapshot();
        Assert.True(expanded.IsExpanded);
        Assert.Equal(7, expanded.VisibleLines.Count);
        Assert.Equal(0, expanded.HiddenCount);
        Assert.Equal(SoftInstrumentChromeAggregator.CollapseLabel, expanded.OverflowLine);

        Assert.False(a.ToggleExpanded());
        var collapsed = a.Snapshot();
        Assert.False(collapsed.IsExpanded);
        Assert.Equal(3, collapsed.VisibleLines.Count);
        Assert.Equal(4, collapsed.HiddenCount);
        Assert.Equal("+4 more · SoftInstrument latches", collapsed.OverflowLine);
    }

    [Fact]
    public void Toggle_noop_when_no_overflow()
    {
        var a = new SoftInstrumentChromeAggregator();
        a.Apply("plan", "only");
        Assert.False(a.ToggleExpanded());
        Assert.False(a.Snapshot().IsExpanded);
        Assert.Null(a.Snapshot().OverflowLine);
    }

    static SoftInstrumentChromeAggregator SeedOverflow()
    {
        var a = new SoftInstrumentChromeAggregator();
        a.Apply("pressure", "p");
        a.Apply("ignite", "i");
        a.Apply("plan", "pl");
        a.Apply("crm", "c");
        a.Apply("learn", "l");
        a.Apply("webcam", "w");
        a.Apply("toolchain", "t");
        return a;
    }
}

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public class EicasBandAggregatorTests
{
    [Fact]
    public void BandStack_shows_all_hot_sources_not_winner_only()
    {
        var a = new EicasBandAggregator();
        a.Apply("alert", "EICAS · WARN · build fail");
        a.Apply("qrh", "EICAS · ADV · QRH-1");
        a.Apply("ecl", "EICAS · ECL · before start");

        Assert.Equal(3, a.BandStack.Count);
        Assert.Contains("WARN", a.BandText!);
        Assert.Contains("QRH-1", a.BandText!);
        Assert.Contains("ECL", a.BandText!);
        Assert.Equal("warn", a.Severity);
        Assert.Equal(3, a.BandChips.Count);
        Assert.Equal("warn", a.BandChips[0].Severity);
        Assert.Equal("adv", a.BandChips[1].Severity);
        Assert.Equal("adv", a.BandChips[2].Severity);
    }

    [Fact]
    public void Clear_source_drops_from_band()
    {
        var a = new EicasBandAggregator();
        a.Apply("alert", "EICAS · CAUT · soft");
        a.Apply("alert", null);
        Assert.Null(a.BandText);
        Assert.Equal("idle", a.Severity);
    }
}

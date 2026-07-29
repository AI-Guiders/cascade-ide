using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Features.Cdp;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpQrhProjectorMapTests
{
    [Fact]
    public void MapMessages_idle_without_hot_is_empty()
    {
        var msgs = CdpQrhProjector.MapMessages(new CdpQrhProjector.QrhLatchDoc
        {
            Pulse = "qrh · idle",
            HotId = null
        });
        Assert.Empty(msgs);
    }

    [Fact]
    public void MapMessages_hot_pulse_is_advisory()
    {
        var msgs = CdpQrhProjector.MapMessages(new CdpQrhProjector.QrhLatchDoc
        {
            Pulse = "qrh · intake-brief +4",
            HotId = "intake-brief",
            HotTitle = "What+why before explore thrash",
            Related =
            [
                new CdpQrhProjector.RelatedPage { Id = "vague-criteria", Title = "Vague ask" }
            ]
        });
        Assert.Equal(2, msgs.Count);
        Assert.Equal(EicasSeverity.Advisory, msgs[0].Severity);
        Assert.Equal("qrh · intake-brief +4", msgs[0].Text);
        Assert.Equal("cdp.qrh", msgs[0].SourceId);
        Assert.Equal("Vague ask", msgs[1].Text);
    }

    [Fact]
    public void LatchEicasFeed_merges_alert_before_qrh()
    {
        var feed = new LatchEicasFeed();
        feed.ReplaceSource("qrh", [new EicasMessage(EicasSeverity.Advisory, "qrh pulse", "cdp.qrh")]);
        feed.ReplaceSource("alert", [new EicasMessage(EicasSeverity.Caution, "*git dirty", "cdp.alert")]);
        var msgs = feed.GetMessages();
        Assert.Equal(2, msgs.Count);
        Assert.Equal("cdp.alert", msgs[0].SourceId);
        Assert.Equal("cdp.qrh", msgs[1].SourceId);
    }
}

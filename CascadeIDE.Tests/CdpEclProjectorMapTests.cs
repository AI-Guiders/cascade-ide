using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Features.Cdp;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpEclProjectorMapTests
{
    [Fact]
    public void MapMessages_idle_without_hot_is_empty()
    {
        Assert.Empty(CdpEclProjector.MapMessages(new CdpEclProjector.EclLatchDoc
        {
            Pulse = "ecl · idle",
            HotId = null,
            OpenRequired = 0
        }));
    }

    [Fact]
    public void MapMessages_hot_clear_still_advisory()
    {
        var msgs = CdpEclProjector.MapMessages(new CdpEclProjector.EclLatchDoc
        {
            Pulse = "ecl · 1 clear",
            HotId = "intake",
            HotTitle = "Before explore",
            OpenRequired = 0
        });
        Assert.Single(msgs);
        Assert.Equal(EicasSeverity.Advisory, msgs[0].Severity);
        Assert.Equal("ecl · 1 clear", msgs[0].Text);
        Assert.Equal("cdp.ecl", msgs[0].SourceId);
    }

    [Fact]
    public void MapMessages_open_items_append()
    {
        var msgs = CdpEclProjector.MapMessages(new CdpEclProjector.EclLatchDoc
        {
            Pulse = "ecl · intake 0/2 (open×2)",
            HotId = "intake",
            HotTitle = "Before explore",
            OpenRequired = 2,
            OpenItems =
            [
                new CdpEclProjector.OpenItem { Id = "project", Text = "cdp_open / project rooted" }
            ]
        });
        Assert.Equal(2, msgs.Count);
        Assert.Equal("cdp_open / project rooted", msgs[1].Text);
    }

    [Fact]
    public void LatchEicasFeed_merges_alert_qrh_ecl()
    {
        var feed = new LatchEicasFeed();
        feed.ReplaceSource("ecl", [new EicasMessage(EicasSeverity.Advisory, "ecl pulse", "cdp.ecl")]);
        feed.ReplaceSource("qrh", [new EicasMessage(EicasSeverity.Advisory, "qrh pulse", "cdp.qrh")]);
        feed.ReplaceSource("alert", [new EicasMessage(EicasSeverity.Caution, "*git dirty", "cdp.alert")]);
        var msgs = feed.GetMessages();
        Assert.Equal(3, msgs.Count);
        Assert.Equal("cdp.alert", msgs[0].SourceId);
        Assert.Equal("cdp.qrh", msgs[1].SourceId);
        Assert.Equal("cdp.ecl", msgs[2].SourceId);
    }
}

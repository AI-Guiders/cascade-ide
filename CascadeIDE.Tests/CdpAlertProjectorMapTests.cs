using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Features.Cdp;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpAlertProjectorMapTests
{
    [Fact]
    public void MapMessages_clear_is_empty()
    {
        var msgs = CdpAlertProjector.MapMessages(new CdpAlertProjector.AlertLatchDoc
        {
            Level = "clear",
            Pulse = "sa clear",
            Lines = ["noise"]
        });
        Assert.Empty(msgs);
    }

    [Fact]
    public void MapMessages_fail_is_warning()
    {
        var msgs = CdpAlertProjector.MapMessages(new CdpAlertProjector.AlertLatchDoc
        {
            Level = "fail",
            Pulse = "sa FAIL",
            Lines = ["*quality F×1"]
        });
        Assert.Single(msgs);
        Assert.Equal(EicasSeverity.Warning, msgs[0].Severity);
        Assert.Equal("*quality F×1", msgs[0].Text);
        Assert.Equal("cdp.alert", msgs[0].SourceId);
    }

    [Fact]
    public void MapMessages_warn_falls_back_to_pulse()
    {
        var msgs = CdpAlertProjector.MapMessages(new CdpAlertProjector.AlertLatchDoc
        {
            Level = "warn",
            Pulse = "sa WARN · gates×1",
            Lines = []
        });
        Assert.Single(msgs);
        Assert.Equal(EicasSeverity.Caution, msgs[0].Severity);
        Assert.Equal("sa WARN · gates×1", msgs[0].Text);
    }
}

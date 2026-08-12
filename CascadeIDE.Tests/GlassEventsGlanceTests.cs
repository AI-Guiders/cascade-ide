#nullable enable
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEventsGlanceTests
{
    [Fact]
    public void Format_idle_when_no_latches()
    {
        var body = GlassEventsGlance.Format(
            new GlassEventsGlance.EventsPresenceStatus(
                LatchLatestCount: 0,
                LatchRoot: @"C:\Users\x\AppData\Local\cdp-mcp",
                Catalog: GlassEventsGlance.DataBusCatalog));

        Assert.Contains("Events glance · IDLE", body);
        Assert.Contains("timeline · Avalonia in-memory", body);
        Assert.Contains("cdp latches · 0", body);
        Assert.Contains("■ Glass latch glance", body);
        Assert.Contains("□ Avalonia EventsMFD", body);
        Assert.Contains("BuildStateChanged", body);
    }

    [Fact]
    public void Format_ready_when_latches_present()
    {
        var body = GlassEventsGlance.Format(
            new GlassEventsGlance.EventsPresenceStatus(
                LatchLatestCount: 12,
                LatchRoot: @"C:\Users\x\AppData\Local\cdp-mcp",
                Catalog: ["BuildStateChanged", "GitStateChanged"]));

        Assert.Contains("Events glance · READY", body);
        Assert.Contains("cdp latches · 12", body);
        Assert.Contains("bus catalog · 2 types", body);
    }
}

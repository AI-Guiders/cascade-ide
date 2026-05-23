using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class InstrumentStatusStripPlacementTests
{
    [Fact]
    public void IsZoneEnabled_whenKeyMissing_defaultsTrue()
    {
        var display = new DisplaySettings { Instruments = new Dictionary<string, string> { ["pfd_primary"] = "workspace_map" } };
        Assert.True(InstrumentStatusStripPlacement.IsVisibleOnPfd(display, masterEnabled: true));
    }

    [Fact]
    public void IsZoneEnabled_whenNone_hidesStrip()
    {
        var display = new DisplaySettings
        {
            Instruments = new Dictionary<string, string>
            {
                [InstrumentRoutingSlotKeys.ForwardStatusStrip] = InstrumentStatusStripRouting.None,
            },
        };
        Assert.False(InstrumentStatusStripPlacement.IsVisibleOnForward(display, masterEnabled: true));
    }

    [Fact]
    public void IsZoneEnabled_whenBackgroundStatus_showsStrip()
    {
        var display = new DisplaySettings
        {
            Instruments = new Dictionary<string, string>
            {
                [InstrumentRoutingSlotKeys.PfdStatusStrip] = "background_status",
            },
        };
        Assert.True(InstrumentStatusStripPlacement.IsVisibleOnPfd(display, masterEnabled: true));
    }

    [Fact]
    public void MasterDisabled_hidesRegardlessOfRouting()
    {
        var display = new DisplaySettings
        {
            Instruments = new Dictionary<string, string>
            {
                [InstrumentRoutingSlotKeys.PfdStatusStrip] = "background_status",
            },
        };
        Assert.False(InstrumentStatusStripPlacement.IsVisibleOnPfd(display, masterEnabled: false));
    }
}

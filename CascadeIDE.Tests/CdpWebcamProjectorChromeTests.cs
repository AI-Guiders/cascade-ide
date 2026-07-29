using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpWebcamProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_webcam_latch/v1", CdpWebcamProjector.Schema);
        Assert.Equal("agent", CdpWebcamProjector.OriginAgent);
        Assert.EndsWith("webcam-LATEST.json", CdpWebcamProjector.LatchPath);
    }

    [Fact]
    public void ApplyWebcamChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentWebcamChromeHint);

        vm.ApplyWebcamChromeHint("webcam · frame · 1280x720");
        Assert.True(vm.ShowAgentWebcamChromeHint);
        Assert.Equal("webcam · frame · 1280x720", vm.AgentWebcamChromeHint);

        vm.ApplyWebcamChromeHint(" ");
        Assert.False(vm.ShowAgentWebcamChromeHint);
        Assert.Null(vm.AgentWebcamChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_webcam_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentWebcamChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentWebcamChromeHint: false));
    }
}

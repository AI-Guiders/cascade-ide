using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpDebugDeskProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_debug_desk_latch/v1", CdpDebugDeskProjector.Schema);
        Assert.Equal("agent", CdpDebugDeskProjector.OriginAgent);
        Assert.EndsWith("debug_desk-LATEST.json", CdpDebugDeskProjector.LatchPath);
    }

    [Fact]
    public void ApplyDebugDeskChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentDebugDeskChromeHint);

        vm.ApplyDebugDeskChromeHint("debug_desk · continue · STOPPED t=1 · bp=2");
        Assert.True(vm.ShowAgentDebugDeskChromeHint);
        Assert.Equal("debug_desk · continue · STOPPED t=1 · bp=2", vm.AgentDebugDeskChromeHint);

        vm.ApplyDebugDeskChromeHint(" ");
        Assert.False(vm.ShowAgentDebugDeskChromeHint);
        Assert.Null(vm.AgentDebugDeskChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_debug_desk_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentDebugDeskChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentDebugDeskChromeHint: false));
    }
}

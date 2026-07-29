using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpIgniteProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_ignite_latch/v1", CdpIgniteProjector.Schema);
        Assert.Equal("agent", CdpIgniteProjector.OriginAgent);
        Assert.EndsWith("ignite-LATEST.json", CdpIgniteProjector.LatchPath);
    }

    [Fact]
    public void ApplyIgniteChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentIgniteChromeHint);

        vm.ApplyIgniteChromeHint("ignite · continuity · armed=1");
        Assert.True(vm.ShowAgentIgniteChromeHint);
        Assert.Equal("ignite · continuity · armed=1", vm.AgentIgniteChromeHint);

        vm.ApplyIgniteChromeHint(" ");
        Assert.False(vm.ShowAgentIgniteChromeHint);
        Assert.Null(vm.AgentIgniteChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_ignite_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false));
    }
}

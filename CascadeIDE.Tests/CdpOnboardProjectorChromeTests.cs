using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpOnboardProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_onboard_latch/v1", CdpOnboardProjector.Schema);
        Assert.Equal("agent", CdpOnboardProjector.OriginAgent);
        Assert.EndsWith("onboard-LATEST.json", CdpOnboardProjector.LatchPath);
    }

    [Fact]
    public void ApplyOnboardChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentOnboardChromeHint);

        vm.ApplyOnboardChromeHint("onboard · cascade-ide · cide · entry=12 · vert=8 · docs=yes");
        Assert.True(vm.ShowAgentOnboardChromeHint);
        Assert.Equal("onboard · cascade-ide · cide · entry=12 · vert=8 · docs=yes", vm.AgentOnboardChromeHint);

        vm.ApplyOnboardChromeHint(" ");
        Assert.False(vm.ShowAgentOnboardChromeHint);
        Assert.Null(vm.AgentOnboardChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_onboard_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false,
            showAgentScopeChromeHint: false,
            showAgentSysChromeHint: false,
            showAgentOnboardChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false,
            showAgentScopeChromeHint: false,
            showAgentSysChromeHint: false,
            showAgentOnboardChromeHint: false));
    }
}

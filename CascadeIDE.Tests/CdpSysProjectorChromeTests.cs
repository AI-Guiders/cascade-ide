using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpSysProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_sys_latch/v1", CdpSysProjector.Schema);
        Assert.Equal("agent", CdpSysProjector.OriginAgent);
        Assert.EndsWith("sys-LATEST.json", CdpSysProjector.LatchPath);
    }

    [Fact]
    public void ApplySysChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentSysChromeHint);

        vm.ApplySysChromeHint("ops · seat=cdp · live=16:01:52Z · staged · armed=1");
        Assert.True(vm.ShowAgentSysChromeHint);
        Assert.Equal("ops · seat=cdp · live=16:01:52Z · staged · armed=1", vm.AgentSysChromeHint);

        vm.ApplySysChromeHint(" ");
        Assert.False(vm.ShowAgentSysChromeHint);
        Assert.Null(vm.AgentSysChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_sys_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false,
            showAgentScopeChromeHint: false,
            showAgentSysChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false,
            showAgentScopeChromeHint: false,
            showAgentSysChromeHint: false));
    }
}

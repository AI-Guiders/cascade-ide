using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpScopeProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_scope_latch/v1", CdpScopeProjector.Schema);
        Assert.Equal("agent", CdpScopeProjector.OriginAgent);
        Assert.EndsWith("scope-LATEST.json", CdpScopeProjector.LatchPath);
    }

    [Fact]
    public void ApplyScopeChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentScopeChromeHint);

        vm.ApplyScopeChromeHint("ps · PRIMARY=cdp-mcp · SCOPE=door-to-singularity");
        Assert.True(vm.ShowAgentScopeChromeHint);
        Assert.Equal("ps · PRIMARY=cdp-mcp · SCOPE=door-to-singularity", vm.AgentScopeChromeHint);

        vm.ApplyScopeChromeHint(" ");
        Assert.False(vm.ShowAgentScopeChromeHint);
        Assert.Null(vm.AgentScopeChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_scope_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false,
            showAgentScopeChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false,
            showAgentIgniteChromeHint: false,
            showAgentScopeChromeHint: false));
    }
}

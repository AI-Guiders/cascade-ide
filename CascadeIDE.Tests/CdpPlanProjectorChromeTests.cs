using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpPlanProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_plan_latch/v1", CdpPlanProjector.Schema);
        Assert.Equal("agent", CdpPlanProjector.OriginAgent);
        Assert.EndsWith("plan-LATEST.json", CdpPlanProjector.LatchPath);
    }

    [Fact]
    public void ApplyPlanChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentPlanChromeHint);

        vm.ApplyPlanChromeHint("Glass › Wire plan · explore");
        Assert.True(vm.ShowAgentPlanChromeHint);
        Assert.Equal("Glass › Wire plan · explore", vm.AgentPlanChromeHint);

        vm.ApplyPlanChromeHint(" ");
        Assert.False(vm.ShowAgentPlanChromeHint);
        Assert.Null(vm.AgentPlanChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_plan_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentMcpChromeHint: false,
            showAgentPlanChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentMcpChromeHint: false,
            showAgentPlanChromeHint: false));
    }
}

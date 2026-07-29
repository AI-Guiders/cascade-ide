using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpCrmProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_crm_latch/v1", CdpCrmProjector.Schema);
        Assert.Equal("agent", CdpCrmProjector.OriginAgent);
        Assert.EndsWith("crm-LATEST.json", CdpCrmProjector.LatchPath);
    }

    [Fact]
    public void ApplyCrmChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentCrmChromeHint);

        vm.ApplyCrmChromeHint("crm · AWAITING · plan:stage-1");
        Assert.True(vm.ShowAgentCrmChromeHint);
        Assert.Equal("crm · AWAITING · plan:stage-1", vm.AgentCrmChromeHint);

        vm.ApplyCrmChromeHint(" ");
        Assert.False(vm.ShowAgentCrmChromeHint);
        Assert.Null(vm.AgentCrmChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_crm_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCrmChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCrmChromeHint: false));
    }
}

using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpLearnProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_learn_latch/v1", CdpLearnProjector.Schema);
        Assert.Equal("agent", CdpLearnProjector.OriginAgent);
        Assert.EndsWith("learn-LATEST.json", CdpLearnProjector.LatchPath);
    }

    [Fact]
    public void ApplyLearnChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentLearnChromeHint);

        vm.ApplyLearnChromeHint("learn · 3 card(s) · go=learn");
        Assert.True(vm.ShowAgentLearnChromeHint);
        Assert.Equal("learn · 3 card(s) · go=learn", vm.AgentLearnChromeHint);

        vm.ApplyLearnChromeHint(" ");
        Assert.False(vm.ShowAgentLearnChromeHint);
        Assert.Null(vm.AgentLearnChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_learn_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentLearnChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentLearnChromeHint: false));
    }
}

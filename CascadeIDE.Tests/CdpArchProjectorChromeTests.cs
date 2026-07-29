using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpArchProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_arch_latch/v1", CdpArchProjector.Schema);
        Assert.Equal("agent", CdpArchProjector.OriginAgent);
        Assert.EndsWith("arch-LATEST.json", CdpArchProjector.LatchPath);
    }

    [Fact]
    public void ApplyArchChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentArchChromeHint);

        vm.ApplyArchChromeHint("as_built · cide · 12 roles · open=3 elect=1 promo=0 · edges=4");
        Assert.True(vm.ShowAgentArchChromeHint);
        Assert.Equal("as_built · cide · 12 roles · open=3 elect=1 promo=0 · edges=4", vm.AgentArchChromeHint);

        vm.ApplyArchChromeHint(" ");
        Assert.False(vm.ShowAgentArchChromeHint);
        Assert.Null(vm.AgentArchChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_arch_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentOnboardChromeHint: false,
            showAgentArchChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentOnboardChromeHint: false,
            showAgentArchChromeHint: false));
    }
}

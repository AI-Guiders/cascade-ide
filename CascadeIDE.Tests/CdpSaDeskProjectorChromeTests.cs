using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpSaDeskProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_sa_desk_latch/v1", CdpSaDeskProjector.Schema);
        Assert.Equal("agent", CdpSaDeskProjector.OriginAgent);
        Assert.EndsWith("sa-desk-LATEST.json", CdpSaDeskProjector.LatchPath);
    }

    [Fact]
    public void ApplySaDeskChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentSaDeskChromeHint);

        vm.ApplySaDeskChromeHint("sa_desk · touch · 2w/0f");
        Assert.True(vm.ShowAgentSaDeskChromeHint);
        Assert.Equal("sa_desk · touch · 2w/0f", vm.AgentSaDeskChromeHint);

        vm.ApplySaDeskChromeHint(" ");
        Assert.False(vm.ShowAgentSaDeskChromeHint);
        Assert.Null(vm.AgentSaDeskChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_sa_desk_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentSaDeskChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentSaDeskChromeHint: false));
    }
}

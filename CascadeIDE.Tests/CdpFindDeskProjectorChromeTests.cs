using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpFindDeskProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_find_desk_latch/v1", CdpFindDeskProjector.Schema);
        Assert.Equal("agent", CdpFindDeskProjector.OriginAgent);
        Assert.EndsWith("find_desk-LATEST.json", CdpFindDeskProjector.LatchPath);
    }

    [Fact]
    public void ApplyFindDeskChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentFindDeskChromeHint);

        vm.ApplyFindDeskChromeHint("find · project · 3 hit(s)");
        Assert.True(vm.ShowAgentFindDeskChromeHint);
        Assert.Equal("find · project · 3 hit(s)", vm.AgentFindDeskChromeHint);

        vm.ApplyFindDeskChromeHint(" ");
        Assert.False(vm.ShowAgentFindDeskChromeHint);
        Assert.Null(vm.AgentFindDeskChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_find_desk_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentFindDeskChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentFindDeskChromeHint: false));
    }
}

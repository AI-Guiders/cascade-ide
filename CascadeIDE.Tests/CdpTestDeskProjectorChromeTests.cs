using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpTestDeskProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_test_desk_latch/v1", CdpTestDeskProjector.Schema);
        Assert.Equal("agent", CdpTestDeskProjector.OriginAgent);
        Assert.EndsWith("test_desk-LATEST.json", CdpTestDeskProjector.LatchPath);
    }

    [Fact]
    public void ApplyTestDeskChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentTestDeskChromeHint);

        vm.ApplyTestDeskChromeHint("test_desk · retest · FAIL 2/5");
        Assert.True(vm.ShowAgentTestDeskChromeHint);
        Assert.Equal("test_desk · retest · FAIL 2/5", vm.AgentTestDeskChromeHint);

        vm.ApplyTestDeskChromeHint(" ");
        Assert.False(vm.ShowAgentTestDeskChromeHint);
        Assert.Null(vm.AgentTestDeskChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_test_desk_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentTestDeskChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentTestDeskChromeHint: false));
    }
}

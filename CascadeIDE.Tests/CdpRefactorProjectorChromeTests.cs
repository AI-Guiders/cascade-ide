using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpRefactorProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_refactor_latch/v1", CdpRefactorProjector.Schema);
        Assert.Equal("agent", CdpRefactorProjector.OriginAgent);
        Assert.EndsWith("refactor-LATEST.json", CdpRefactorProjector.LatchPath);
    }

    [Fact]
    public void ApplyRefactorChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentRefactorChromeHint);

        vm.ApplyRefactorChromeHint("refactor · hotspots=2 · go=refactor");
        Assert.True(vm.ShowAgentRefactorChromeHint);
        Assert.Equal("refactor · hotspots=2 · go=refactor", vm.AgentRefactorChromeHint);

        vm.ApplyRefactorChromeHint(" ");
        Assert.False(vm.ShowAgentRefactorChromeHint);
        Assert.Null(vm.AgentRefactorChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_refactor_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentRefactorChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentRefactorChromeHint: false));
    }
}

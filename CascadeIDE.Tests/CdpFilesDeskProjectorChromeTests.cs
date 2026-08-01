using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpFilesDeskProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_files_desk_latch/v1", CdpFilesDeskProjector.Schema);
        Assert.Equal("agent", CdpFilesDeskProjector.OriginAgent);
        Assert.EndsWith("files_desk-LATEST.json", CdpFilesDeskProjector.LatchPath);
    }

    [Fact]
    public void ApplyFilesDeskChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentFilesDeskChromeHint);

        vm.ApplyFilesDeskChromeHint("files · project · cascade-ide · 12");
        Assert.True(vm.ShowAgentFilesDeskChromeHint);
        Assert.Equal("files · project · cascade-ide · 12", vm.AgentFilesDeskChromeHint);

        vm.ApplyFilesDeskChromeHint(" ");
        Assert.False(vm.ShowAgentFilesDeskChromeHint);
        Assert.Null(vm.AgentFilesDeskChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_files_desk_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentFilesDeskChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentFilesDeskChromeHint: false));
    }
}

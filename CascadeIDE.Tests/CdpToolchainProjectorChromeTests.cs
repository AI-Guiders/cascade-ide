using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpToolchainProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_toolchain_latch/v1", CdpToolchainProjector.Schema);
        Assert.Equal("agent", CdpToolchainProjector.OriginAgent);
        Assert.EndsWith("toolchain-LATEST.json", CdpToolchainProjector.LatchPath);
    }

    [Fact]
    public void ApplyToolchainChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentToolchainChromeHint);

        vm.ApplyToolchainChromeHint("toolchain · 3/5 ok · go=toolchain");
        Assert.True(vm.ShowAgentToolchainChromeHint);
        Assert.Equal("toolchain · 3/5 ok · go=toolchain", vm.AgentToolchainChromeHint);

        vm.ApplyToolchainChromeHint(" ");
        Assert.False(vm.ShowAgentToolchainChromeHint);
        Assert.Null(vm.AgentToolchainChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_toolchain_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentToolchainChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentToolchainChromeHint: false));
    }
}

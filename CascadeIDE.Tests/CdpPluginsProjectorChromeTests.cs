using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpPluginsProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_plugins_latch/v1", CdpPluginsProjector.Schema);
        Assert.Equal("agent", CdpPluginsProjector.OriginAgent);
        Assert.EndsWith("plugins-LATEST.json", CdpPluginsProjector.LatchPath);
    }

    [Fact]
    public void ApplyPluginsChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentPluginsChromeHint);

        vm.ApplyPluginsChromeHint("plugins · 2 attn (1 Mode A) · go=plugins");
        Assert.True(vm.ShowAgentPluginsChromeHint);
        Assert.Equal("plugins · 2 attn (1 Mode A) · go=plugins", vm.AgentPluginsChromeHint);

        vm.ApplyPluginsChromeHint(" ");
        Assert.False(vm.ShowAgentPluginsChromeHint);
        Assert.Null(vm.AgentPluginsChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_plugins_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentPluginsChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentPluginsChromeHint: false));
    }
}

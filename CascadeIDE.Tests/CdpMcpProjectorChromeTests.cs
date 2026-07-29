using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpMcpProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_mcp_latch/v1", CdpMcpProjector.Schema);
        Assert.Equal("agent", CdpMcpProjector.OriginAgent);
        Assert.EndsWith("mcp-LATEST.json", CdpMcpProjector.LatchPath);
    }

    [Fact]
    public void ApplyMcpChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentMcpChromeHint);

        vm.ApplyMcpChromeHint("mcp · 2 mounted");
        Assert.True(vm.ShowAgentMcpChromeHint);
        Assert.Equal("mcp · 2 mounted", vm.AgentMcpChromeHint);

        vm.ApplyMcpChromeHint(" ");
        Assert.False(vm.ShowAgentMcpChromeHint);
        Assert.Null(vm.AgentMcpChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_mcp_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentArchChromeHint: false,
            showAgentMcpChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentArchChromeHint: false,
            showAgentMcpChromeHint: false));
    }
}

using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpPressureProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_pressure_latch/v1", CdpPressureProjector.Schema);
        Assert.Equal("agent", CdpPressureProjector.OriginAgent);
        Assert.EndsWith("pressure-LATEST.json", CdpPressureProjector.LatchPath);
    }

    [Fact]
    public void ApplyPressureChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentPressureChromeHint);

        vm.ApplyPressureChromeHint("pressure · ARMED · stashed");
        Assert.True(vm.ShowAgentPressureChromeHint);
        Assert.Equal("pressure · ARMED · stashed", vm.AgentPressureChromeHint);

        vm.ApplyPressureChromeHint(" ");
        Assert.False(vm.ShowAgentPressureChromeHint);
        Assert.Null(vm.AgentPressureChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_pressure_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false,
            showAgentPressureChromeHint: false));
    }
}

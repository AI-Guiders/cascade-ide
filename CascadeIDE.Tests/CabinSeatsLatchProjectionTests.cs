using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Schema + packing chrome for seats-LATEST (CDP writer / CIDE reader).</summary>
public class CabinSeatsLatchProjectionTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_seats_latch/v1", CdpSeatsProjector.Schema);
        Assert.Equal("agent", CdpSeatsProjector.OriginAgent);
        Assert.Equal(CdpHabitatPaths.SeatsLatchFileName, Path.GetFileName(CdpSeatsProjector.LatchPath));
        Assert.EndsWith(CdpHabitatPaths.SeatsLatchFileName, CdpSeatsProjector.LatchPath);
    }

    [Fact]
    public void ApplyCabinOrganChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentCabinChromeHint);

        vm.ApplyCabinOrganChromeHint("agent · M: pressure");
        Assert.True(vm.ShowAgentCabinChromeHint);
        Assert.Equal("agent · M: pressure", vm.AgentCabinChromeHint);

        vm.ApplyCabinOrganChromeHint(" ");
        Assert.False(vm.ShowAgentCabinChromeHint);
        Assert.Null(vm.AgentCabinChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_cabin_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentCabinChromeHint: false));
    }
}

using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpReportProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_report_latch/v1", CdpReportProjector.Schema);
        Assert.Equal("agent", CdpReportProjector.OriginAgent);
        Assert.EndsWith("report-LATEST.json", CdpReportProjector.LatchPath);
    }

    [Fact]
    public void ApplyReportChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentReportChromeHint);

        vm.ApplyReportChromeHint("report · check ok · scratch.csx");
        Assert.True(vm.ShowAgentReportChromeHint);
        Assert.Equal("report · check ok · scratch.csx", vm.AgentReportChromeHint);

        vm.ApplyReportChromeHint(" ");
        Assert.False(vm.ShowAgentReportChromeHint);
        Assert.Null(vm.AgentReportChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_report_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentReportChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentReportChromeHint: false));
    }
}

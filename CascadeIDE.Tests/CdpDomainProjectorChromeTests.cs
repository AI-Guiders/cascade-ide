using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpDomainProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_domain_latch/v1", CdpDomainProjector.Schema);
        Assert.Equal("agent", CdpDomainProjector.OriginAgent);
        Assert.EndsWith("domain-LATEST.json", CdpDomainProjector.LatchPath);
    }

    [Fact]
    public void ApplyDomainChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentDomainChromeHint);

        vm.ApplyDomainChromeHint("domain · 4 cards · go=domain");
        Assert.True(vm.ShowAgentDomainChromeHint);
        Assert.Equal("domain · 4 cards · go=domain", vm.AgentDomainChromeHint);

        vm.ApplyDomainChromeHint(" ");
        Assert.False(vm.ShowAgentDomainChromeHint);
        Assert.Null(vm.AgentDomainChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_domain_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentDomainChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentDomainChromeHint: false));
    }
}

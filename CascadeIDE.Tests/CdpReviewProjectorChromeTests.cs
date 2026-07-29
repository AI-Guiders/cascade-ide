using CascadeIDE.Features.Cdp;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class CdpReviewProjectorChromeTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_review_latch/v1", CdpReviewProjector.Schema);
        Assert.Equal("agent", CdpReviewProjector.OriginAgent);
        Assert.EndsWith("review-LATEST.json", CdpReviewProjector.LatchPath);
    }

    [Fact]
    public void ApplyReviewChromeHint_sets_quiet_band()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.ShowAgentReviewChromeHint);

        vm.ApplyReviewChromeHint("review · ready ×3 · go=review");
        Assert.True(vm.ShowAgentReviewChromeHint);
        Assert.Equal("review · ready ×3 · go=review", vm.AgentReviewChromeHint);

        vm.ApplyReviewChromeHint(" ");
        Assert.False(vm.ShowAgentReviewChromeHint);
        Assert.Null(vm.AgentReviewChromeHint);
    }

    [Fact]
    public void ShowWorkspaceChromeBand_includes_review_hint()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentReviewChromeHint: true));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            showIdeHealthStrip: false,
            showEicasAlertsBar: false,
            showAgentReviewChromeHint: false));
    }
}

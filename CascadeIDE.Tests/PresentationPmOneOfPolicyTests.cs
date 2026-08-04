using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class PresentationPmOneOfPolicyTests
{
    [Fact]
    public void FromMfdPage_requests_M()
    {
        Assert.Equal(PresentationAnchorKind.Mfd, PresentationPmOneOfPolicy.FromMfdPage("Editor"));
        Assert.Null(PresentationPmOneOfPolicy.FromMfdPage(" "));
        Assert.Null(PresentationPmOneOfPolicy.FromMfdPage(null));
    }

    [Fact]
    public void FromPlanLatch_does_not_steal_OneOf()
    {
        Assert.Null(PresentationPmOneOfPolicy.FromPlanLatch());
    }

    [Fact]
    public void SeatsMaySelectMfd_never_auto_switches()
    {
        Assert.False(PresentationPmOneOfPolicy.SeatsMaySelectMfd(
            stickyMfdPage: null,
            seatsMfdPage: "WebAiPortal",
            seatsMOrganChanged: true));
        Assert.False(PresentationPmOneOfPolicy.SeatsMaySelectMfd(
            stickyMfdPage: "Editor",
            seatsMfdPage: "Terminal",
            seatsMOrganChanged: true));
        Assert.False(PresentationPmOneOfPolicy.SeatsMaySelectMfd(
            stickyMfdPage: null,
            seatsMfdPage: "Editor",
            seatsMOrganChanged: false));
    }

    [Fact]
    public void Toggle_xor_P_and_M()
    {
        Assert.Equal(
            PresentationAnchorKind.Mfd,
            PresentationPmOneOfPolicy.Toggle(PresentationAnchorKind.Pfd));
        Assert.Equal(
            PresentationAnchorKind.Pfd,
            PresentationPmOneOfPolicy.Toggle(PresentationAnchorKind.Mfd));
    }

    [Theory]
    [InlineData("Terminal", "shell")]
    [InlineData("Git", "git")]
    [InlineData("WebAiPortal", "world")]
    [InlineData("DomainBoard", "sit")]
    [InlineData("Events", "alert")]
    [InlineData("DebugStack", "probe")]
    public void PreferSurfaceFromMfdPage_maps_intent(string page, string surface) =>
        Assert.Equal(surface, PresentationPmOneOfPolicy.PreferSurfaceFromMfdPage(page));

    [Fact]
    public void ResolveStackSurface_exact_then_zone_fallback()
    {
        string[] stack = ["sit", "world", "alert"];
        Assert.Equal("world", PresentationPmOneOfPolicy.ResolveStackSurface(stack, "WebAiPortal"));
        Assert.Equal("sit", PresentationPmOneOfPolicy.ResolveStackSurface(stack, "DomainBoard"));
        // Terminal→shell not in stack; zone(shell)=Mfd → first Mfd paint in stack = world
        Assert.Equal("world", PresentationPmOneOfPolicy.ResolveStackSurface(stack, "Terminal"));
        Assert.Null(PresentationPmOneOfPolicy.ResolveStackSurface(stack, "Editor")); // work→Forward, not in P/M stack zones
    }
}

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
    public void SeatsMaySelectMfd_blocks_PF_republish_when_sticky_differs()
    {
        Assert.False(PresentationPmOneOfPolicy.SeatsMaySelectMfd(
            stickyMfdPage: "Editor",
            seatsMfdPage: "WebAiPortal",
            seatsMOrganChanged: false));
    }

    [Fact]
    public void SeatsMaySelectMfd_allows_when_M_organ_changed()
    {
        Assert.True(PresentationPmOneOfPolicy.SeatsMaySelectMfd(
            stickyMfdPage: "Editor",
            seatsMfdPage: "Terminal",
            seatsMOrganChanged: true));
    }

    [Fact]
    public void SeatsMaySelectMfd_allows_same_as_sticky()
    {
        Assert.True(PresentationPmOneOfPolicy.SeatsMaySelectMfd(
            stickyMfdPage: "Editor",
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
}

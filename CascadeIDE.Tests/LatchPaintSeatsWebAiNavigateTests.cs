#nullable enable
using CascadeIDE.GlassCore.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LatchPaintSeatsWebAiNavigateTests
{
    [Fact]
    public void Sticky_web_ai_url_with_non_browser_show_face_does_not_want_navigate()
    {
        var wants = SeatsWebAiNavigateGate.WantsNavigate(
            showFace: true,
            webAiUrl: "https://news.ycombinator.com/",
            mfdPage: "RelatedFiles",
            faceOrgan: "find_desk",
            mOrgan: "browser",
            faceSeat: "p");
        Assert.False(wants);
    }

    [Fact]
    public void Browser_face_with_web_ai_url_wants_navigate()
    {
        var wants = SeatsWebAiNavigateGate.WantsNavigate(
            showFace: true,
            webAiUrl: "https://news.ycombinator.com/",
            mfdPage: "WebAiPortal",
            faceOrgan: "browser",
            mOrgan: "browser",
            faceSeat: "m");
        Assert.True(wants);
    }

    [Fact]
    public void M_face_on_browser_organ_wants_navigate_even_if_mfd_not_webai()
    {
        var wants = SeatsWebAiNavigateGate.WantsNavigate(
            showFace: true,
            webAiUrl: "https://example.com/",
            mfdPage: null,
            faceOrgan: null,
            mOrgan: "browser",
            faceSeat: "m");
        Assert.True(wants);
    }
}

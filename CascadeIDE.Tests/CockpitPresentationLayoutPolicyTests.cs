using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CockpitPresentationLayoutPolicyTests
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings(
            screenMarkers: "()",
            screenSeparator: " ",
            zoneSeparator: "+",
            pfdZoneIdentifier: "P",
            forwardZoneIdentifier: "F",
            mfdZoneIdentifier: "M");

    [Fact]
    public void RequiresPfd_WhenDedicatedMfdSecondScreen_FirstScreenHasP()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(CockpitPresentationLayoutPolicy.RequiresVisiblePfdColumn(r));
    }

    [Fact]
    public void RequiresMfdInMain_WhenSingleScreenWeightedP_F_M_True()
    {
        var r = PresentationParser.Parse("(0.2P+0.3F+0.5M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(CockpitPresentationLayoutPolicy.RequiresExpandedChatColumnForMainWindow(r));
    }

    [Fact]
    public void RequiresMfdInMain_WhenDedicatedSecondScreen_False()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.False(CockpitPresentationLayoutPolicy.RequiresExpandedChatColumnForMainWindow(r));
    }

    [Fact]
    public void CoerceSolutionExplorer_FalseWhenPfdRequired_BecomesTrue()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(CockpitPresentationLayoutPolicy.CoerceSolutionExplorerVisible(r, false));
    }

    [Fact]
    public void CoerceChat_FalseWhenMOnFirstScreen_BecomesTrue()
    {
        var r = PresentationParser.Parse("(P+F+M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(CockpitPresentationLayoutPolicy.CoerceChatPanelExpanded(r, false));
    }

    [Fact]
    public void CoerceChat_FalseWhenNoMOnFirstScreen_Unchanged()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.False(CockpitPresentationLayoutPolicy.CoerceChatPanelExpanded(r, false));
    }
}

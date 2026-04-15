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
        Assert.True(CockpitPresentationLayoutPolicy.RequiresPfdRegionInMainWindow(r));
    }

    [Fact]
    public void RequiresMfdInMain_WhenSingleScreenWeightedP_F_M_True()
    {
        var r = PresentationParser.Parse("(0.2P+0.3F+0.5M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(CockpitPresentationLayoutPolicy.RequiresMfdRegionInMainWindow(r));
    }

    [Fact]
    public void RequiresMfdInMain_WhenDedicatedSecondScreen_False()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.False(CockpitPresentationLayoutPolicy.RequiresMfdRegionInMainWindow(r));
    }

    [Fact]
    public void CoercePfdRegion_FalseWhenPfdRequired_BecomesTrue()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(CockpitPresentationLayoutPolicy.CoercePfdRegionExpanded(r, false));
    }

    [Fact]
    public void CoerceMfdRegion_FalseWhenMOnFirstScreen_BecomesTrue()
    {
        var r = PresentationParser.Parse("(P+F+M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(CockpitPresentationLayoutPolicy.CoerceMfdRegionExpanded(r, false));
    }

    [Fact]
    public void CoerceMfdRegion_FalseWhenNoMOnFirstScreen_Unchanged()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.False(CockpitPresentationLayoutPolicy.CoerceMfdRegionExpanded(r, false));
    }
}

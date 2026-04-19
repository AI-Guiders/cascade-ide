using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationLayoutAnalyzerTests
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings(
            brackets: "()",
            betweenScreens: " ",
            betweenZones: "+",
            pfdZoneIdentifier: "P",
            forwardZoneIdentifier: "F",
            mfdZoneIdentifier: "M");

    [Fact]
    public void IsPfdForwardCombinedOnFirstScreen_WhenXPYFAndM_True()
    {
        var r = PresentationParser.Parse("(0.25P + 0.75F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.IsPfdForwardCombinedOnFirstScreen(r.Screens));
    }

    [Fact]
    public void IsPfdForwardCombinedOnFirstScreen_WhenTripleScreensP_F_M_False()
    {
        var r = PresentationParser.Parse("(P) (F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.False(PresentationLayoutAnalyzer.IsPfdForwardCombinedOnFirstScreen(r.Screens));
    }

    [Fact]
    public void ShouldMaximizeMainWindowAtStartup_WhenTripleP_F_M_True()
    {
        var r = PresentationParser.Parse("(P) (F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup(r.Screens));
    }

    [Fact]
    public void ShouldMaximizeMainWindowAtStartup_WhenSingleScreenWeightedP_F_M_True()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(0.2P+0.3F+0.5M)", g);
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup(r.Screens));
    }

    [Fact]
    public void IsPfdForwardCombinedOnFirstScreen_WhenOnlyP_False()
    {
        var r = PresentationParser.Parse("(P)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.False(PresentationLayoutAnalyzer.IsPfdForwardCombinedOnFirstScreen(r.Screens));
    }
}

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

    [Fact]
    public void TryGetMainWindowPresentationScreenIndex_TripleP_F_M_ForwardIsScreen1()
    {
        var r = PresentationParser.Parse("(P) (F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.TryGetMainWindowPresentationScreenIndex(r.Screens, out var idx));
        Assert.Equal(1, idx);
        Assert.Equal(1, PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(r));
    }

    [Fact]
    public void TryGetMainWindowPresentationScreenIndex_TriplePermuted_M_F_P_ForwardIsScreen1()
    {
        var r = PresentationParser.Parse("(M) (F) (P)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.TryGetMainWindowPresentationScreenIndex(r.Screens, out var idx));
        Assert.Equal(1, idx);
    }

    [Fact]
    public void IsForwardMfdTwoScreenPreset_WhenF_M_True()
    {
        var r = PresentationParser.Parse("(F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.IsForwardMfdTwoScreenPreset(r.Screens));
        Assert.True(PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup(r.Screens));
        Assert.True(PresentationLayoutAnalyzer.TryGetMainWindowPresentationScreenIndex(r.Screens, out var mainIdx));
        Assert.Equal(0, mainIdx);
        Assert.True(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(r.Screens, out var mfdIdx));
        Assert.Equal(1, mfdIdx);
    }

    [Fact]
    public void IsForwardMfdTwoScreenPreset_WhenM_F_Symmetric()
    {
        var r = PresentationParser.Parse("(M) (F)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.IsForwardMfdTwoScreenPreset(r.Screens));
        Assert.True(PresentationLayoutAnalyzer.TryGetMainWindowPresentationScreenIndex(r.Screens, out var mainIdx));
        Assert.Equal(1, mainIdx);
        Assert.True(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(r.Screens, out var mfdIdx));
        Assert.Equal(0, mfdIdx);
    }
}

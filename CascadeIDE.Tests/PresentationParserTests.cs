using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationParserTests
{
    private static readonly PresentationGrammarTokens Default = PresentationGrammarTokens.Default;

    [Fact]
    public void Empty_Yields_NoScreens()
    {
        var r = PresentationParser.Parse("", Default);
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Screens);
    }

    [Fact]
    public void SingleScreen_ThreeAnchors_Plus()
    {
        var r = PresentationParser.Parse("(PFD+Forward+MFD)", Default);
        Assert.True(r.IsSuccess);
        Assert.Single(r.Screens);
        Assert.Equal(
            new[] { PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd },
            r.Screens[0]);
    }

    [Fact]
    public void SingleScreen_ThreeAnchors_WithZoneSeparator_Pipe()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "|");
        var r = PresentationParser.Parse("(PFD|Forward|MFD)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(
            new[] { PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd },
            r.Screens[0]);
    }

    [Fact]
    public void DefaultZoneSeparator_Plus_PipeBetweenAnchors_IsInvalid()
    {
        var r = PresentationParser.Parse("(PFD|Forward|MFD)", Default);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void TwoScreens_DedicatedMfd()
    {
        var r = PresentationParser.Parse("(PFD+Forward) (MFD)", Default);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Screens.Count);
        Assert.Equal(new[] { PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward }, r.Screens[0]);
        Assert.Equal(new[] { PresentationAnchorKind.Mfd }, r.Screens[1]);
        Assert.True(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(r.Screens, out var mfdIdx));
        Assert.Equal(1, mfdIdx);
    }

    [Fact]
    public void ThreeScreens_Pfd_Forward_Mfd_Yields_MfdIndex2()
    {
        var r = PresentationParser.Parse("(PFD) (Forward) (MFD)", Default);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Screens.Count);
        Assert.True(PresentationLayoutAnalyzer.IsTriplePfdForwardMfdPreset(r.Screens));
        Assert.True(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(r.Screens, out var mfdIdx));
        Assert.Equal(2, mfdIdx);
    }

    [Fact]
    public void TwoScreens_ShortIdentifiers_P_F_M()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(P+F) (M)", g);
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(r.Screens));
    }

    [Fact]
    public void CustomScreenMarkers()
    {
        var g = PresentationGrammarTokens.FromSettings("[]", " ", "+");
        var r = PresentationParser.Parse("[PFD+Forward] [MFD]", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Screens.Count);
    }

    [Fact]
    public void LegacySpellings_forward_Mfd_AreInvalid()
    {
        Assert.False(PresentationParser.Parse("(PFD+forward+MFD)", Default).IsSuccess);
        Assert.False(PresentationParser.Parse("(PFD+Forward+Mfd)", Default).IsSuccess);
        Assert.False(PresentationParser.Parse("(PFD+Forward+mfd)", Default).IsSuccess);
    }

    [Fact]
    public void CustomZoneIdentifiers_FromSettings()
    {
        var g = PresentationGrammarTokens.FromSettings(
            "()", " ", "+",
            pfdZoneIdentifier: "Pfd",
            forwardZoneIdentifier: "Lob",
            mfdZoneIdentifier: "Mfd");
        var r = PresentationParser.Parse("(Pfd+Lob+Mfd)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(
            new[] { PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd },
            r.Screens[0]);
    }

    [Fact]
    public void ShortZoneIdentifiers_SingleLetter()
    {
        var g = PresentationGrammarTokens.FromSettings(
            "()", " ", "+",
            pfdZoneIdentifier: "p",
            forwardZoneIdentifier: "l",
            mfdZoneIdentifier: "m");
        var r = PresentationParser.Parse("(p+l+m)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(
            new[] { PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd },
            r.Screens[0]);
    }

    [Fact]
    public void DuplicateZoneIdentifiers_FallbackToDefaultIdentifiers()
    {
        var g = PresentationGrammarTokens.FromSettings(
            "()", " ", "+",
            pfdZoneIdentifier: "P",
            forwardZoneIdentifier: "P",
            mfdZoneIdentifier: "MFD");
        var r = PresentationParser.Parse("(PFD+Forward+MFD)", g);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Invalid_UnknownToken()
    {
        var r = PresentationParser.Parse("(PFD+oops)", Default);
        Assert.False(r.IsSuccess);
        Assert.NotNull(r.Error);
    }
}

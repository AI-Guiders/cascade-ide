using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationParserTests
{
    private static readonly PresentationGrammarTokens Default = PresentationGrammarTokens.Default;

    private static PresentationAnchorSlot[] Unweighted(params PresentationAnchorKind[] kinds) =>
        kinds.Select(k => new PresentationAnchorSlot(k, null)).ToArray();

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
        Assert.Equal(Unweighted(PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd), r.Screens[0]);
    }

    [Fact]
    public void SingleScreen_ThreeAnchors_WithZoneSeparator_Pipe()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "|");
        var r = PresentationParser.Parse("(PFD|Forward|MFD)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(Unweighted(PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd), r.Screens[0]);
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
        Assert.Equal(Unweighted(PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward), r.Screens[0]);
        Assert.Equal(Unweighted(PresentationAnchorKind.Mfd), r.Screens[1]);
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
        Assert.True(PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(r.Screens));
        Assert.True(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(r.Screens, out var mfdIdx));
        Assert.Equal(2, mfdIdx);
        Assert.True(PresentationLayoutAnalyzer.TryGetPfdHostPresentationScreenIndex(r.Screens, out var pfdIdx));
        Assert.Equal(0, pfdIdx);
    }

    [Fact]
    public void ThreeScreens_permuted_M_F_P_resolves_anchor_screen_indices()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(M) (F) (P)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Screens.Count);
        Assert.True(PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(r.Screens));
        Assert.False(PresentationLayoutAnalyzer.IsTriplePfdForwardMfdPreset(r.Screens));
        Assert.True(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(r.Screens, out var mIdx));
        Assert.Equal(0, mIdx);
        Assert.True(PresentationLayoutAnalyzer.TryGetPfdHostPresentationScreenIndex(r.Screens, out var pIdx));
        Assert.Equal(2, pIdx);
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
    public void TwoScreens_Weighted_FirstGroup()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(0.25P+0.75F)(M)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Screens.Count);
        Assert.Equal(
            new[]
            {
                new PresentationAnchorSlot(PresentationAnchorKind.Pfd, 0.25),
                new PresentationAnchorSlot(PresentationAnchorKind.Forward, 0.75),
            },
            r.Screens[0]);
        Assert.Equal(Unweighted(PresentationAnchorKind.Mfd), r.Screens[1]);
        Assert.True(PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(r.Screens));
    }

    [Fact]
    public void TwoScreens_Weighted_FirstGroup_SpacesInsideScreenMarkers_NormalizedLikeCompactForm()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var compact = PresentationParser.Parse("(0.25P+0.75F)(M)", g);
        var spaced = PresentationParser.Parse("(0.25P + 0.75F) ( M )", g);
        Assert.True(spaced.IsSuccess);
        Assert.True(compact.IsSuccess);
        Assert.Equal(compact.Screens, spaced.Screens);
    }

    [Fact]
    public void SingleScreen_ThreeAnchors_Weighted()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(0.2P+0.3F+0.5M)", g);
        Assert.True(r.IsSuccess);
        Assert.Equal(
            new[]
            {
                new PresentationAnchorSlot(PresentationAnchorKind.Pfd, 0.2),
                new PresentationAnchorSlot(PresentationAnchorKind.Forward, 0.3),
                new PresentationAnchorSlot(PresentationAnchorKind.Mfd, 0.5),
            },
            r.Screens[0]);
    }

    [Fact]
    public void Weighted_SumNotOne_Fails()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(0.5P+0.5F+0.5M)", g);
        Assert.False(r.IsSuccess);
        Assert.Contains("Сумма коэффициентов", r.Error ?? "");
    }

    [Fact]
    public void MixedWeightedAndUnweighted_Fails()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(0.25P+F+M)", g);
        Assert.False(r.IsSuccess);
        Assert.Contains("Смешение", r.Error ?? "");
    }

    [Fact]
    public void SingleAnchor_WithWeight_Fails()
    {
        var g = PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");
        var r = PresentationParser.Parse("(1M)", g);
        Assert.False(r.IsSuccess);
        Assert.Contains("Один якорь", r.Error ?? "");
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
        Assert.Equal(Unweighted(PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd), r.Screens[0]);
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
        Assert.Equal(Unweighted(PresentationAnchorKind.Pfd, PresentationAnchorKind.Forward, PresentationAnchorKind.Mfd), r.Screens[0]);
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

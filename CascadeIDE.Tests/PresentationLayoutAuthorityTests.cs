using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationLayoutAuthorityTests
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
        Assert.True(PresentationLayoutAuthority.RequiresVisiblePfdColumn(r));
    }

    [Fact]
    public void RequiresMfdInMain_WhenSingleScreenWeightedP_F_M_True()
    {
        var r = PresentationParser.Parse("(0.2P+0.3F+0.5M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAuthority.RequiresExpandedChatColumnForMainWindow(r));
    }

    [Fact]
    public void RequiresMfdInMain_WhenDedicatedSecondScreen_False()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.False(PresentationLayoutAuthority.RequiresExpandedChatColumnForMainWindow(r));
    }

    [Fact]
    public void CoerceSolutionExplorer_FalseWhenPfdRequired_BecomesTrue()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(PresentationLayoutAuthority.CoerceSolutionExplorerVisible(r, false));
    }

    [Fact]
    public void CoerceChat_FalseWhenMOnFirstScreen_BecomesTrue()
    {
        var r = PresentationParser.Parse("(P+F+M)", DefaultGrammar());
        Assert.True(r.IsSuccess);
        Assert.True(PresentationLayoutAuthority.CoerceChatPanelExpanded(r, false));
    }

    [Fact]
    public void CoerceChat_FalseWhenNoMOnFirstScreen_Unchanged()
    {
        var r = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.False(PresentationLayoutAuthority.CoerceChatPanelExpanded(r, false));
    }
}

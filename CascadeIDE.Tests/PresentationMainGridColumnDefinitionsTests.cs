using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationMainGridColumnDefinitionsTests
{
    private static readonly PresentationGrammarTokens ShortGrammar =
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    [Fact]
    public void NoWeights_UsesDefault()
    {
        var parse = PresentationParser.Parse("(P+F+M)", ShortGrammar);
        Assert.True(parse.IsSuccess);
        var s = PresentationMainGridColumnDefinitions.Get(parse, false, false, false, false);
        Assert.Equal(PresentationMainGridColumnDefinitions.Default, s);
    }

    [Fact]
    public void TripleWeighted_StarColumns()
    {
        var parse = PresentationParser.Parse("(0.2P+0.3F+0.5M)", ShortGrammar);
        Assert.True(parse.IsSuccess);
        var s = PresentationMainGridColumnDefinitions.Get(parse, false, false, false, false);
        Assert.Equal("0.2*,4,0.3*,4,0.5*", s);
    }

    [Fact]
    public void DualWeighted_FixedMfdTail()
    {
        var parse = PresentationParser.Parse("(0.25P+0.75F)(M)", ShortGrammar);
        Assert.True(parse.IsSuccess);
        var s = PresentationMainGridColumnDefinitions.Get(
            parse,
            dedicatedMfdSecondScreen: true,
            mfdColumnSuppressedForHost: false,
            tripleOneAnchorPerZone: false,
            suppressPfdColumnForPfdHostWindow: false);
        Assert.Equal("0.25*,4,0.75*,4,340", s);
    }

    [Fact]
    public void DualWeighted_HostSuppressesMfdColumn_ZeroTail()
    {
        var parse = PresentationParser.Parse("(0.25P+0.75F)(M)", ShortGrammar);
        Assert.True(parse.IsSuccess);
        var s = PresentationMainGridColumnDefinitions.Get(
            parse,
            dedicatedMfdSecondScreen: true,
            mfdColumnSuppressedForHost: true,
            tripleOneAnchorPerZone: false,
            suppressPfdColumnForPfdHostWindow: false);
        Assert.Equal("0.25*,4,0.75*,4,0", s);
    }
}

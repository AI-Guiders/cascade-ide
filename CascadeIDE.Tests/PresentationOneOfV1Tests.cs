using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class PresentationOneOfV1Tests
{
    static readonly PresentationGrammarTokens Grammar = PresentationGrammarTokens.Default;

    [Theory]
    [InlineData("(F)(P/M)", "F", "P", "M", 0, 1)]
    [InlineData("(P/M)(F)", "F", "P", "M", 1, 0)]
    [InlineData("(P)(F/M)", "P", "F", "M", 0, 1)]
    [InlineData("(F/M)(P)", "P", "F", "M", 1, 0)]
    [InlineData("(M)(P/F)", "M", "P", "F", 0, 1)]
    [InlineData("(P/F)(M)", "M", "P", "F", 1, 0)]
    public void Describe_Any_Pair_Plus_Dedicated(
        string line,
        string dedicatedTok,
        string aTok,
        string bTok,
        int dedicatedScreen,
        int oneOfScreen)
    {
        var parse = PresentationParser.Parse(line, Grammar);
        Assert.True(parse.IsSuccess, parse.Error);
        Assert.True(
            PresentationLayoutAnalyzer.TryDescribeOneOfPlusDedicatedTwoScreen(
                parse.Screens,
                parse.ScreenComposes,
                out var dedicated,
                out var a,
                out var b,
                out var dedIdx,
                out var oneIdx));

        Assert.Equal(ParseTok(dedicatedTok), dedicated);
        Assert.True(
            a == ParseTok(aTok) && b == ParseTok(bTok)
            || a == ParseTok(bTok) && b == ParseTok(aTok));
        Assert.Equal(dedicatedScreen, dedIdx);
        Assert.Equal(oneOfScreen, oneIdx);
        Assert.True(PresentationLayoutAnalyzer.IsOneOfPlusDedicatedTwoScreenPreset(parse.Screens, parse.ScreenComposes));
    }

    [Theory]
    [InlineData("(F)(P/M)", true)]
    [InlineData("(P)(F/M)", false)]
    [InlineData("(M)(P/F)", false)]
    public void PmOneOf_Subset_Only_When_Dedicated_Forward(string line, bool pmSubset)
    {
        var parse = PresentationParser.Parse(line, Grammar);
        Assert.Equal(
            pmSubset,
            PresentationLayoutAnalyzer.IsPmOneOfForwardTwoScreenPreset(parse.Screens, parse.ScreenComposes));

        var flags = PresentationTopologyResolver.ResolveFlags(parse);
        Assert.True(flags.OneOfHostTopology);
        Assert.Equal(pmSubset, flags.PmOneOfHostTopology);
    }

    [Theory]
    [InlineData("(P)(F/M)", 1)]
    [InlineData("(M)(P/F)", 1)]
    [InlineData("(F)(P/M)", 0)]
    public void Main_Is_Screen_Containing_Forward(string line, int mainScreen)
    {
        var parse = PresentationParser.Parse(line, Grammar);
        Assert.Equal(
            mainScreen,
            PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(parse));
    }

    [Theory]
    [InlineData(PresentationOneOfChannelPolicy.Sit, PresentationAnchorKind.Pfd)]
    [InlineData(PresentationOneOfChannelPolicy.Work, PresentationAnchorKind.Forward)]
    [InlineData(PresentationOneOfChannelPolicy.World, PresentationAnchorKind.Mfd)]
    public void Channel_Maps_To_Anchor(string channel, PresentationAnchorKind want) =>
        Assert.Equal(want, PresentationOneOfChannelPolicy.AnchorForChannel(channel));

    [Fact]
    public void Alert_Does_Not_Steal_OneOf()
    {
        Assert.Null(PresentationOneOfChannelPolicy.AnchorForChannel(PresentationOneOfChannelPolicy.Alert));
        Assert.Null(
            PresentationOneOfChannelPolicy.PreferOneOfForChannel(
                PresentationOneOfChannelPolicy.Alert,
                PresentationAnchorKind.Pfd,
                PresentationAnchorKind.Mfd));
    }

    [Fact]
    public void PreferOneOf_Only_When_Face_In_Set()
    {
        Assert.Equal(
            PresentationAnchorKind.Mfd,
            PresentationOneOfChannelPolicy.PreferOneOfForChannel(
                PresentationOneOfChannelPolicy.World,
                PresentationAnchorKind.Pfd,
                PresentationAnchorKind.Mfd));
        Assert.Null(
            PresentationOneOfChannelPolicy.PreferOneOfForChannel(
                PresentationOneOfChannelPolicy.Work,
                PresentationAnchorKind.Pfd,
                PresentationAnchorKind.Mfd));
    }

    static PresentationAnchorKind ParseTok(string t) => t switch
    {
        "P" => PresentationAnchorKind.Pfd,
        "F" => PresentationAnchorKind.Forward,
        "M" => PresentationAnchorKind.Mfd,
        _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
    };
}

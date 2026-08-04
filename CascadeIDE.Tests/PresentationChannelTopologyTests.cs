using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class PresentationChannelTopologyTests
{
    static readonly PresentationGrammarTokens Grammar = PresentationGrammarTokens.Default;

    [Theory]
    [InlineData("(F)(P/M)", PresentationChannelId.Work, PresentationChannelId.Sit, PresentationChannelId.World)]
    [InlineData("(P/M)(F)", PresentationChannelId.Work, PresentationChannelId.Sit, PresentationChannelId.World)]
    [InlineData("(P)(F/M)", PresentationChannelId.Sit, PresentationChannelId.Work, PresentationChannelId.World)]
    [InlineData("(M)(P/F)", PresentationChannelId.World, PresentationChannelId.Sit, PresentationChannelId.Work)]
    public void Meta_Wire_Describes_Channel_Pack(
        string line,
        PresentationChannelId dedicated,
        PresentationChannelId oneA,
        PresentationChannelId oneB)
    {
        var parse = PresentationParser.Parse(line, Grammar);
        Assert.True(parse.IsSuccess, parse.Error);
        Assert.True(
            PresentationChannelTopology.TryDescribeChannelPackFromMetaWire(
                parse.Screens,
                parse.ScreenComposes,
                out var dedCh,
                out var aCh,
                out var bCh,
                out _,
                out _,
                out _,
                out _,
                out _));

        Assert.Equal(dedicated, dedCh);
        Assert.True(
            aCh == oneA && bCh == oneB || aCh == oneB && bCh == oneA,
            $"got {aCh}/{bCh}");
    }

    [Fact]
    public void Scan_Stays_Channels_Stack_Defaults()
    {
        Assert.Equal(PresentationChannelId.Sit, PresentationChannelTopology.DefaultChannelOnScan(PresentationZoneMeta.P));
        Assert.Equal(PresentationChannelId.Work, PresentationChannelTopology.DefaultChannelOnScan(PresentationZoneMeta.F));
        Assert.Equal(PresentationChannelId.World, PresentationChannelTopology.DefaultChannelOnScan(PresentationZoneMeta.M));
        Assert.Equal(PresentationZoneMeta.F, PresentationChannelTopology.DefaultScanForChannel(PresentationChannelId.Work));
        Assert.Null(PresentationChannelTopology.DefaultScanForChannel(PresentationChannelId.Alert));
    }

    [Fact]
    public void Prefer_Is_By_Channel_Face_Meta()
    {
        Assert.Equal(
            PresentationZoneMeta.M,
            PresentationChannelTopology.PreferMetaForChannel(
                PresentationChannelId.World,
                PresentationZoneMeta.P,
                PresentationZoneMeta.M));
        Assert.Null(
            PresentationChannelTopology.PreferMetaForChannel(
                PresentationChannelId.Work,
                PresentationZoneMeta.P,
                PresentationZoneMeta.M));
        Assert.Null(
            PresentationChannelTopology.PreferMetaForChannel(
                PresentationChannelId.Alert,
                PresentationZoneMeta.P,
                PresentationZoneMeta.M));
    }

    [Theory]
    [InlineData("(F)(P/M)", true)]
    [InlineData("(P)(F/M)", false)]
    public void PmOneOf_Is_Only_Meta_Compat_Subset(string line, bool pmSubset)
    {
        var parse = PresentationParser.Parse(line, Grammar);
        Assert.Equal(
            pmSubset,
            PresentationLayoutAnalyzer.IsPmOneOfForwardTwoScreenPreset(parse.Screens, parse.ScreenComposes));
        Assert.True(
            PresentationChannelTopology.TryDescribeChannelPackFromMetaWire(
                parse.Screens, parse.ScreenComposes,
                out _, out _, out _, out _, out _, out _, out _, out _));
    }
}

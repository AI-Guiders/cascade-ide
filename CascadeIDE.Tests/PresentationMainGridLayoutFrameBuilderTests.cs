using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationMainGridLayoutFrameBuilderTests
{
    private static readonly PresentationGrammarTokens ShortGrammar =
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    [Fact]
    public void TripleWeighted_BuildsWeightedColumns()
    {
        var parse = PresentationParser.Parse("(0.2P+0.3F+0.5M)", ShortGrammar);
        Assert.True(parse.IsSuccess);

        var frame = PresentationMainGridLayoutFrameBuilder.Build(
            parse,
            dedicatedMfdSecondScreen: false,
            mfdColumnSuppressedForHost: false);

        Assert.Equal("0.2*,0.3*,0.5*", frame.ColumnDefinitions);
        Assert.Equal(3, frame.ContentZoneCount);
        Assert.True(frame.HasExplicitWeights);
        Assert.Equal(3, frame.NormalizedZoneWeights.Count);
        Assert.Equal(3, frame.ZoneBounds.Count);
        Assert.Equal(PresentationAnchorKind.Pfd, frame.ZoneBounds[0].Zone);
        Assert.Equal(0.0, frame.ZoneBounds[0].StartNormalized, 8);
        Assert.Equal(0.2, frame.ZoneBounds[0].WidthNormalized, 8);
        Assert.Equal(PresentationAnchorKind.Forward, frame.ZoneBounds[1].Zone);
        Assert.Equal(0.2, frame.ZoneBounds[1].StartNormalized, 8);
        Assert.Equal(0.3, frame.ZoneBounds[1].WidthNormalized, 8);
        Assert.Equal(PresentationAnchorKind.Mfd, frame.ZoneBounds[2].Zone);
        Assert.Equal(0.5, frame.ZoneBounds[2].StartNormalized, 8);
        Assert.Equal(0.5, frame.ZoneBounds[2].WidthNormalized, 8);
    }

    [Fact]
    public void DualWeighted_DedicatedAndSuppressed_UsesZeroTail()
    {
        var parse = PresentationParser.Parse("(0.25P+0.75F)(M)", ShortGrammar);
        Assert.True(parse.IsSuccess);

        var frame = PresentationMainGridLayoutFrameBuilder.Build(
            parse,
            dedicatedMfdSecondScreen: true,
            mfdColumnSuppressedForHost: true);

        Assert.Equal("0.25*,0.75*,0", frame.ColumnDefinitions);
        Assert.Equal(2, frame.ContentZoneCount);
        Assert.True(frame.HasExplicitWeights);
        Assert.Equal(2, frame.ZoneBounds.Count);
        Assert.Equal(PresentationAnchorKind.Pfd, frame.ZoneBounds[0].Zone);
        Assert.Equal(0.0, frame.ZoneBounds[0].StartNormalized, 8);
        Assert.Equal(0.25, frame.ZoneBounds[0].WidthNormalized, 8);
        Assert.Equal(PresentationAnchorKind.Forward, frame.ZoneBounds[1].Zone);
        Assert.Equal(0.25, frame.ZoneBounds[1].StartNormalized, 8);
        Assert.Equal(0.75, frame.ZoneBounds[1].WidthNormalized, 8);
    }

    [Fact]
    public void NoWeights_UsesDefaultColumns_AndProvidesNormalizedShares()
    {
        var parse = PresentationParser.Parse("(P+F+M)", ShortGrammar);
        Assert.True(parse.IsSuccess);

        var frame = PresentationMainGridLayoutFrameBuilder.Build(
            parse,
            dedicatedMfdSecondScreen: false,
            mfdColumnSuppressedForHost: false);

        Assert.Equal(PresentationMainGridLayoutFrameBuilder.DefaultColumnDefinitions, frame.ColumnDefinitions);
        Assert.Equal(3, frame.ContentZoneCount);
        Assert.False(frame.HasExplicitWeights);
        Assert.Equal(1.0 / 3.0, frame.NormalizedZoneWeights[0], 8);
        Assert.Equal(1.0 / 3.0, frame.NormalizedZoneWeights[1], 8);
        Assert.Equal(1.0 / 3.0, frame.NormalizedZoneWeights[2], 8);
        Assert.Equal(3, frame.ZoneBounds.Count);
    }
}


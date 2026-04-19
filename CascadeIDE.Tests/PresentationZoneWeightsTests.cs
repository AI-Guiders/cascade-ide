using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationZoneWeightsTests
{
    [Fact]
    public void TryNormalize_NoWeights_ReturnsEqualShares()
    {
        var screen = new[]
        {
            new PresentationAnchorSlot(PresentationAnchorKind.Pfd, null),
            new PresentationAnchorSlot(PresentationAnchorKind.Forward, null),
            new PresentationAnchorSlot(PresentationAnchorKind.Mfd, null),
        };

        var ok = PresentationZoneWeights.TryNormalize(screen, out var weights);

        Assert.True(ok);
        Assert.Equal(3, weights.Count);
        Assert.Equal(1.0 / 3.0, weights[0], 8);
        Assert.Equal(1.0 / 3.0, weights[1], 8);
        Assert.Equal(1.0 / 3.0, weights[2], 8);
    }

    [Fact]
    public void TryNormalize_WithWeights_NormalizesToOne()
    {
        var screen = new[]
        {
            new PresentationAnchorSlot(PresentationAnchorKind.Pfd, 0.6),
            new PresentationAnchorSlot(PresentationAnchorKind.Forward, 0.3),
            new PresentationAnchorSlot(PresentationAnchorKind.Mfd, 0.1),
        };

        var ok = PresentationZoneWeights.TryNormalize(screen, out var weights);

        Assert.True(ok);
        Assert.Equal(3, weights.Count);
        Assert.Equal(0.6, weights[0], 8);
        Assert.Equal(0.3, weights[1], 8);
        Assert.Equal(0.1, weights[2], 8);
        Assert.Equal(1.0, weights[0] + weights[1] + weights[2], 8);
    }

    [Fact]
    public void MainGridDefinitions_UseNormalizedWeightedShares_ForTriple()
    {
        var parse = PresentationParseResult.Ok(
            new[]
            {
                (IReadOnlyList<PresentationAnchorSlot>)new[]
                {
                    new PresentationAnchorSlot(PresentationAnchorKind.Pfd, 0.5),
                    new PresentationAnchorSlot(PresentationAnchorKind.Forward, 0.3),
                    new PresentationAnchorSlot(PresentationAnchorKind.Mfd, 0.2),
                }
            });

        var result = PresentationMainGridColumnDefinitions.Get(
            parse,
            dedicatedMfdSecondScreen: false,
            mfdColumnSuppressedForHost: false,
            tripleOneAnchorPerZone: false,
            suppressPfdColumnForPfdHostWindow: false);

        Assert.Equal("0.5*,4,0.3*,4,0.2*", result);
    }
}


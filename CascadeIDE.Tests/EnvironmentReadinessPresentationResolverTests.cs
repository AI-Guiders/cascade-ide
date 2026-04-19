using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EnvironmentReadinessPresentationResolverTests
{
    [Fact]
    public void Resolve_ZeroOrNegativeWidth_IsCompactCards()
    {
        Assert.Equal(EnvironmentReadinessPresentationKind.CompactCards,
            EnvironmentReadinessPresentationResolver.Resolve(0));
        Assert.Equal(EnvironmentReadinessPresentationKind.CompactCards,
            EnvironmentReadinessPresentationResolver.Resolve(-1));
    }

    [Fact]
    public void Resolve_BelowThreshold_IsCompactCards()
    {
        Assert.Equal(EnvironmentReadinessPresentationKind.CompactCards,
            EnvironmentReadinessPresentationResolver.Resolve(
                EnvironmentReadinessPresentationResolver.DefaultWideLayoutMinWidthPx - 1));
    }

    [Fact]
    public void Resolve_AtOrAboveThreshold_IsWideTable()
    {
        Assert.Equal(EnvironmentReadinessPresentationKind.WideTable,
            EnvironmentReadinessPresentationResolver.Resolve(
                EnvironmentReadinessPresentationResolver.DefaultWideLayoutMinWidthPx));
        Assert.Equal(EnvironmentReadinessPresentationKind.WideTable,
            EnvironmentReadinessPresentationResolver.Resolve(800));
    }
}

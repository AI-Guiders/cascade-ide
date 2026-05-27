using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GraphFileLayoutMetricsTests
{
    [Theory]
    [InlineData(4, CodeNavigationMapDetailLevel.Normal, CodeNavigationMapRelatedGraphLayoutKind.Radial, 120)]
    [InlineData(12, CodeNavigationMapDetailLevel.Normal, CodeNavigationMapRelatedGraphLayoutKind.Radial, 142)]
    [InlineData(8, CodeNavigationMapDetailLevel.Normal, CodeNavigationMapRelatedGraphLayoutKind.TopDown, 380)]
    public void EstimatePreferredHeight(
        int satellites,
        CodeNavigationMapDetailLevel detail,
        string layout,
        double expectedMin)
    {
        var h = GraphFileLayoutMetrics.EstimatePreferredHeight(satellites, detail, layout);
        Assert.True(h >= expectedMin - 0.5);
    }

    [Fact]
    public void ResolveRadialOrbits_two_rings_outer_exceeds_inner()
    {
        var (inner, outer) = GraphFileLayoutMetrics.ResolveRadialOrbits(18, 360, 220, 13);
        Assert.True(outer > inner);
        Assert.True(inner >= 26);
    }
}

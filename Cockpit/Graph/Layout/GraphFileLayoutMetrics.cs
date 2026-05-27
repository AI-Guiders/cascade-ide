#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Геометрия related-files на карте намерений (уровень file).</summary>
public static class GraphFileLayoutMetrics
{
    public const double SideLabelMargin = 68;
    public const double DefaultHeightFile = GraphViewportMetrics.DefaultHeightFile;
    public const double MaxHeightFile = 380;
    public const double MaxHeightHierarchy = 520;

    public static double EstimatePreferredHeight(
        int satelliteCount,
        CodeNavigationMapDetailLevel detailLevel,
        string relatedLayout)
    {
        if (satelliteCount <= 0)
            return DefaultHeightFile;

        var layout = CodeNavigationMapRelatedGraphLayoutKind.Normalize(relatedLayout);
        if (CodeNavigationMapRelatedGraphLayoutKind.IsHierarchy(layout))
            return EstimateHierarchyHeight(satelliteCount, detailLevel);

        if (satelliteCount <= 8)
            return DefaultHeightFile;

        var perSatellite = detailLevel switch
        {
            CodeNavigationMapDetailLevel.Glance => 3.5,
            CodeNavigationMapDetailLevel.Inspect => 7.5,
            _ => 5.5
        };
        var extra = (satelliteCount - 8) * perSatellite;
        return Math.Clamp(DefaultHeightFile + extra, DefaultHeightFile, MaxHeightFile);
    }

    public static double EstimateHierarchyHeight(int satelliteCount, CodeNavigationMapDetailLevel detailLevel)
    {
        var perRow = detailLevel switch
        {
            CodeNavigationMapDetailLevel.Glance => 34,
            CodeNavigationMapDetailLevel.Inspect => 44,
            _ => 38
        };
        var rows = 1 + Math.Max(0, satelliteCount);
        return Math.Clamp(72 + rows * perRow, DefaultHeightFile, MaxHeightHierarchy);
    }

    /// <summary>Радиусы внутренней и внешней орбит (режим <c>radial</c>).</summary>
    public static (double Inner, double Outer) ResolveRadialOrbits(
        int satelliteCount,
        double innerWidth,
        double innerHeight,
        double satelliteRadius)
    {
        var minInner = Math.Min(innerWidth, innerHeight);
        var extent = Math.Sqrt(Math.Max(1, innerWidth * innerHeight));
        var wantTwoRings = satelliteCount > 8;
        var densityBoost = 1.0 + Math.Max(0, satelliteCount - 6) * 0.042;
        var maxOuterOrbit = Math.Max(32, minInner * 0.50 - satelliteRadius);
        var desired = extent * (wantTwoRings ? 0.40 : 0.44) * densityBoost;
        var useTwoRings = wantTwoRings && maxOuterOrbit >= 56;

        if (!useTwoRings)
        {
            var orbit = ClampOrMin(desired, 26, maxOuterOrbit);
            return (orbit, orbit);
        }

        var maxInnerOrbit = Math.Max(26, maxOuterOrbit - satelliteRadius * 3.2 - 14);
        var orbitInner = ClampOrMin(desired * 0.9, 26, maxInnerOrbit);
        var orbitOuterDesired = Math.Max(
            orbitInner + satelliteRadius * 2.6 + 14,
            extent * 0.52 * densityBoost);
        var orbitOuter = ClampOrMin(
            orbitOuterDesired,
            orbitInner + satelliteRadius + 8,
            maxOuterOrbit);
        return (orbitInner, orbitOuter);
    }

    private static double ClampOrMin(double value, double min, double max) =>
        max < min ? min : Math.Clamp(value, min, max);
}

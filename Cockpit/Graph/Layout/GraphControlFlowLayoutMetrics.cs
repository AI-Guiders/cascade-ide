#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Геометрия и политика укладки control-flow graph-backed surface (ADR 0055).</summary>
public static class GraphControlFlowLayoutMetrics
{
    public const double IntrinsicHeightBasePx = 28;

    public static double VerticalSpacingForDetailLevel(CodeNavigationMapDetailLevel detailLevel) =>
        detailLevel switch
        {
            CodeNavigationMapDetailLevel.Glance => 26,
            CodeNavigationMapDetailLevel.Inspect => 36,
            _ => 32,
        };

    public static double EstimatePreferredHeight(int estimatedLevelCount, CodeNavigationMapDetailLevel detailLevel)
    {
        var spacing = VerticalSpacingForDetailLevel(detailLevel);
        var levelBands = Math.Max(1, estimatedLevelCount);
        return IntrinsicHeightBasePx + Math.Max(0, levelBands - 1) * spacing;
    }

    public const double TopPadding = 10;
    public const double BottomPadding = 10;
    public const double SidePadding = 10;
    public const double LegendGap = 2;
    public const double BesideLegendInkSlack = 4;
    public const double LegendBesideMinClearance = 6;
    public const double LegendSideColumnMinTextWidth = 100;
    public const double LegendBelowBlockGap = 6;
    public const double MinGraphHeightForBelowLegend = 50;
    public const double MinGraphWidth = 40;
    public const double MaxReadableBandWidth = 380;
    public const double LegendReserveWidthFraction = 0.30;
    public const double LegendReserveMin = 88;
    public const double LegendReserveHardCap = 340;

    public static double ResolveLegendReserveCap(double viewportWidth) =>
        Math.Min(LegendReserveHardCap, viewportWidth * 0.52);

    public const double RefVerticalStep = 34;
    public const double MaxReadableVerticalStep = 40;
    public const double MaxReadableVerticalStepCap = 88;
    public const double AnchorRadiusBase = 14;
    public const double NodeRadiusBase = 12;
    public const double RadiusScaleMin = 0.4;
    public const double RadiusScaleMax = 1.12;
    public const double HorizontalRadiusScaleMin = 0.74;

    public static double MinVerticalStepForLevelCount(int levelCount) =>
        levelCount switch
        {
            >= 16 => 10,
            >= 12 => 12,
            >= 9 => 14,
            >= 6 => 16,
            _ => 18,
        };

    public const int LabelMaxLength = 22;
    public const int LabelTruncateLength = 19;
    public const int LabelCharBudgetMin = 8;

    public static double ResolveReadableBandWidth(double graphWidth) =>
        Math.Clamp(graphWidth, MinGraphWidth, MaxReadableBandWidth);

    public static int ResolveLabelCharBudget(double bandWidth)
    {
        var approx = (int)Math.Floor((bandWidth - 52) / 7.0);
        return Math.Clamp(approx, LabelCharBudgetMin, LabelMaxLength);
    }

    public static double ResolveSideLabelFontSize(double bandWidth, double verticalStep)
    {
        var widthFactor = Math.Clamp(bandWidth / 220.0, 0.68, 1.06);
        var stepFactor = Math.Clamp(verticalStep / RefVerticalStep, 0.82, 1.08);
        var combined = GraphRenderInvariants.MinSideLabelFontSize * widthFactor * stepFactor;
        return Math.Clamp(
            combined,
            GraphRenderInvariants.CompactSideLabelFontSizeFloor,
            GraphRenderInvariants.MaxSideLabelFontSize);
    }

    public static double EstimateLegendBlockHeight(
        int rowCount,
        bool hasShapeKeys,
        int edgeStyleKeyRowCount = 0,
        double captionSize = 11)
    {
        var lineH = Math.Max(15, captionSize * 1.2);
        const double keyRowHBase = 17d;
        var keyRowH = Math.Max(keyRowHBase, captionSize + 5);
        const double gapBeforeKeys = 6d;
        var shapeKeyBlockH = hasShapeKeys ? (keyRowH * 3 + gapBeforeKeys + 8) : 0d;
        var edgeStyleBlockH = edgeStyleKeyRowCount > 0
            ? edgeStyleKeyRowCount * keyRowH + 8d
            : 0d;
        const double betweenEdgeAndShape = 6d;
        var betweenBlocks = 0d;
        if (edgeStyleKeyRowCount > 0 && hasShapeKeys)
            betweenBlocks = betweenEdgeAndShape;
        var keyBlockH = edgeStyleBlockH + betweenBlocks + shapeKeyBlockH;
        var rowBlock = rowCount * lineH;
        var hasAnyKeyBlock = hasShapeKeys || edgeStyleKeyRowCount > 0;
        if (rowCount == 0 && hasAnyKeyBlock)
            return keyBlockH + 12d;
        var between = rowCount > 0 && hasAnyKeyBlock ? gapBeforeKeys : 0d;
        return rowBlock + between + keyBlockH + 12d;
    }
}

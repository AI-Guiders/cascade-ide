#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Геометрия и политика укладки control-flow graph-backed surface (ADR 0055).</summary>
public static class GraphControlFlowLayoutMetrics
{
    public const double IntrinsicHeightBasePx = 32;

    public static double VerticalSpacingForDetailLevel(CodeNavigationMapDetailLevel detailLevel) =>
        detailLevel switch
        {
            CodeNavigationMapDetailLevel.Glance => 30,
            CodeNavigationMapDetailLevel.Inspect => 40,
            _ => 36,
        };

    public static double EstimatePreferredHeight(int estimatedLevelCount, CodeNavigationMapDetailLevel detailLevel)
    {
        var spacing = VerticalSpacingForDetailLevel(detailLevel);
        var levelBands = Math.Max(1, estimatedLevelCount);
        return IntrinsicHeightBasePx + Math.Max(0, levelBands - 1) * spacing;
    }

    public const double TopPadding = 12;
    public const double BottomPadding = 12;
    public const double SidePadding = 12;
    public const double LegendGap = 2;
    public const double BesideLegendInkSlack = 4;
    public const double LegendBesideMinClearance = 6;
    public const double LegendSideColumnMinTextWidth = 100;
    public const double LegendBelowBlockGap = 6;
    public const double MinGraphHeightForBelowLegend = 50;
    public const double MinGraphWidth = 40;
    public const double MaxReadableBandWidth = 400;
    public const double LegendReserveWidthFraction = 0.30;
    public const double LegendReserveMin = 88;
    public const double LegendReserveHardCap = 340;

    public static double ResolveLegendReserveCap(double viewportWidth) =>
        Math.Min(LegendReserveHardCap, viewportWidth * 0.52);

    public const double RefVerticalStep = 38;
    public const double MaxReadableVerticalStep = 44;
    public const double MaxReadableVerticalStepCap = 96;
    public const double AnchorRadiusBase = 15.5;
    public const double NodeRadiusBase = 13.5;
    public const double RadiusScaleMin = 0.4;
    public const double RadiusScaleMax = 1.12;
    public const double HorizontalRadiusScaleMin = 0.74;

    public static double MinVerticalStepForLevelCount(int levelCount) =>
        levelCount switch
        {
            >= 16 => 12,
            >= 12 => 14,
            >= 9 => 16,
            >= 6 => 18,
            _ => 20,
        };

    /// <summary>Ось потока: приоритет — уместить шаги по главной оси; иначе соотношение сторон и «длина цепочки vs ширина уровня».</summary>
    public static GraphControlFlowMainAxis ChooseMainAxis(
        double graphWidth,
        double heightForLayout,
        int levelCount,
        int maxNodesOnAnyLevel)
    {
        var innerH = heightForLayout - TopPadding - BottomPadding;
        var innerW = graphWidth - 2 * SidePadding;
        if (innerH < 1 || innerW < 1 || levelCount < 1)
            return GraphControlFlowMainAxis.Vertical;

        var slotCount = Math.Max(1, levelCount - 1);
        var minStep = MinVerticalStepForLevelCount(levelCount);
        var slackMainIfVertical = innerH / slotCount;
        var slackMainIfHorizontal = innerW / slotCount;

        if (slackMainIfHorizontal >= minStep && slackMainIfVertical < minStep * 0.92)
            return GraphControlFlowMainAxis.Horizontal;
        if (slackMainIfVertical >= minStep && slackMainIfHorizontal < minStep * 0.92)
            return GraphControlFlowMainAxis.Vertical;

        var aspect = innerW / innerH;

        if (aspect >= 1.18 && slackMainIfHorizontal >= minStep * 0.85)
            return GraphControlFlowMainAxis.Horizontal;
        if (aspect <= 0.85 && slackMainIfVertical >= minStep * 0.85)
            return GraphControlFlowMainAxis.Vertical;

        if (Math.Abs(slackMainIfHorizontal - slackMainIfVertical) > 5)
            return slackMainIfHorizontal > slackMainIfVertical
                ? GraphControlFlowMainAxis.Horizontal
                : GraphControlFlowMainAxis.Vertical;

        var depthHeavy = slotCount > Math.Max(2, maxNodesOnAnyLevel);
        if (depthHeavy && slackMainIfHorizontal >= minStep && innerW >= innerH)
            return GraphControlFlowMainAxis.Horizontal;

        return innerW > innerH * 1.02
            ? GraphControlFlowMainAxis.Horizontal
            : GraphControlFlowMainAxis.Vertical;
    }

    /// <summary>
    /// Не-null — ось из <c>[code_navigation_map].control_flow_main_axis</c>; <see langword="null"/> для <c>auto</c> (см. <see cref="ChooseMainAxis"/>).
    /// </summary>
    public static GraphControlFlowMainAxis? TryControlFlowMainAxisOverride(string normalizedSetting) =>
        normalizedSetting switch
        {
            CodeNavigationMapControlFlowMainAxisKind.Vertical => GraphControlFlowMainAxis.Vertical,
            CodeNavigationMapControlFlowMainAxisKind.Horizontal => GraphControlFlowMainAxis.Horizontal,
            _ => null,
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

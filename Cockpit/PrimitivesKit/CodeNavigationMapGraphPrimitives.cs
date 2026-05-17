using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Legacy forwarders к <see cref="GraphViewportMetrics"/> / <see cref="GraphControlFlowLayoutMetrics"/>.
/// </summary>
public static class CodeNavigationMapGraphPrimitives
{
    public const double DefaultViewportWidth = GraphViewportMetrics.DefaultWidth;
    public const double DefaultViewportHeightFile = GraphViewportMetrics.DefaultHeightFile;
    public const double DefaultViewportHeightControlFlow = GraphViewportMetrics.DefaultHeightControlFlow;
    public const double MaxViewportHeightControlFlow = GraphViewportMetrics.MaxHeightControlFlow;

    public const double ControlFlowIntrinsicHeightBasePx = GraphControlFlowLayoutMetrics.IntrinsicHeightBasePx;

    public static double VerticalSpacingForDetailLevel(CodeNavigationMapDetailLevel detailLevel) =>
        GraphControlFlowLayoutMetrics.VerticalSpacingForDetailLevel(detailLevel);

    public static double EstimateControlFlowPreferredHeight(int estimatedLevelCount, CodeNavigationMapDetailLevel detailLevel) =>
        GraphControlFlowLayoutMetrics.EstimatePreferredHeight(estimatedLevelCount, detailLevel);

    public const double ControlFlowTopPadding = GraphControlFlowLayoutMetrics.TopPadding;
    public const double ControlFlowBottomPadding = GraphControlFlowLayoutMetrics.BottomPadding;
    public const double ControlFlowSidePadding = GraphControlFlowLayoutMetrics.SidePadding;
    public const double ControlFlowLegendGap = GraphControlFlowLayoutMetrics.LegendGap;
    public const double ControlFlowBesideLegendInkSlack = GraphControlFlowLayoutMetrics.BesideLegendInkSlack;
    public const double ControlFlowLegendBesideMinClearance = GraphControlFlowLayoutMetrics.LegendBesideMinClearance;
    public const double ControlFlowLegendSideColumnMinTextWidth = GraphControlFlowLayoutMetrics.LegendSideColumnMinTextWidth;
    public const double ControlFlowLegendBelowBlockGap = GraphControlFlowLayoutMetrics.LegendBelowBlockGap;
    public const double ControlFlowMinGraphHeightForBelowLegend = GraphControlFlowLayoutMetrics.MinGraphHeightForBelowLegend;
    public const double ControlFlowMinGraphWidth = GraphControlFlowLayoutMetrics.MinGraphWidth;
    public const double ControlFlowMaxReadableBandWidth = GraphControlFlowLayoutMetrics.MaxReadableBandWidth;
    public const double ControlFlowLegendReserveWidthFraction = GraphControlFlowLayoutMetrics.LegendReserveWidthFraction;
    public const double ControlFlowLegendReserveMin = GraphControlFlowLayoutMetrics.LegendReserveMin;
    public const double ControlFlowLegendReserveHardCap = GraphControlFlowLayoutMetrics.LegendReserveHardCap;

    public static double ResolveControlFlowLegendReserveCap(double viewportWidth) =>
        GraphControlFlowLayoutMetrics.ResolveLegendReserveCap(viewportWidth);

    public const double ControlFlowRefVerticalStep = GraphControlFlowLayoutMetrics.RefVerticalStep;
    public const double ControlFlowMaxReadableVerticalStep = GraphControlFlowLayoutMetrics.MaxReadableVerticalStep;
    public const double ControlFlowMaxReadableVerticalStepCap = GraphControlFlowLayoutMetrics.MaxReadableVerticalStepCap;
    public const double ControlFlowAnchorRadiusBase = GraphControlFlowLayoutMetrics.AnchorRadiusBase;
    public const double ControlFlowNodeRadiusBase = GraphControlFlowLayoutMetrics.NodeRadiusBase;
    public const double ControlFlowRadiusScaleMin = GraphControlFlowLayoutMetrics.RadiusScaleMin;
    public const double ControlFlowRadiusScaleMax = GraphControlFlowLayoutMetrics.RadiusScaleMax;
    public const double ControlFlowHorizontalRadiusScaleMin = GraphControlFlowLayoutMetrics.HorizontalRadiusScaleMin;

    public static double MinVerticalStepForLevelCount(int levelCount) =>
        GraphControlFlowLayoutMetrics.MinVerticalStepForLevelCount(levelCount);

    public const int ControlFlowLabelMaxLength = GraphControlFlowLayoutMetrics.LabelMaxLength;
    public const int ControlFlowLabelTruncateLength = GraphControlFlowLayoutMetrics.LabelTruncateLength;
    public const int ControlFlowLabelCharBudgetMin = GraphControlFlowLayoutMetrics.LabelCharBudgetMin;

    public static double ResolveControlFlowReadableBandWidth(double graphWidth) =>
        GraphControlFlowLayoutMetrics.ResolveReadableBandWidth(graphWidth);

    public static int ResolveControlFlowLabelCharBudget(double bandWidth) =>
        GraphControlFlowLayoutMetrics.ResolveLabelCharBudget(bandWidth);

    public static double ResolveControlFlowSideLabelFontSize(double bandWidth, double verticalStep) =>
        GraphControlFlowLayoutMetrics.ResolveSideLabelFontSize(bandWidth, verticalStep);

    public static double EstimateControlFlowLegendBlockHeight(
        int rowCount,
        bool hasShapeKeys,
        int edgeStyleKeyRowCount = 0,
        double captionSize = 11) =>
        GraphControlFlowLayoutMetrics.EstimateLegendBlockHeight(rowCount, hasShapeKeys, edgeStyleKeyRowCount, captionSize);
}

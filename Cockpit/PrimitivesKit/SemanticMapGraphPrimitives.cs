using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Единые токены геометрии и viewport для Semantic Map (ADR 0055, 0056): пайплайн композиции и раскладка control-flow.
/// </summary>
public static class SemanticMapGraphPrimitives
{
    #region Viewport / слот инструмента

    public const double DefaultViewportWidth = 280;
    public const double DefaultViewportHeightFile = 120;
    public const double DefaultViewportHeightControlFlow = 220;
    /// <summary>Верхний предел интринсик-высоты control-flow при слиянии с viewport.</summary>
    public const double MaxViewportHeightControlFlow = 640;

    #endregion

    #region Политика интринсик-высоты control-flow (layout stage)

    public const double ControlFlowIntrinsicHeightBasePx = 28;

    public static double VerticalSpacingForDetailLevel(SemanticMapDetailLevel detailLevel) =>
        detailLevel switch
        {
            SemanticMapDetailLevel.Glance => 26,
            SemanticMapDetailLevel.Inspect => 36,
            _ => 32,
        };

    /// <summary>Оценка предпочтительной высоты панели по числу уровней и детализации (до clamp к <see cref="MaxViewportHeightControlFlow"/>).</summary>
    public static double EstimateControlFlowPreferredHeight(int estimatedLevelCount, SemanticMapDetailLevel detailLevel)
    {
        var spacing = VerticalSpacingForDetailLevel(detailLevel);
        var levelBands = Math.Max(1, estimatedLevelCount);
        return ControlFlowIntrinsicHeightBasePx + Math.Max(0, levelBands - 1) * spacing;
    }

    #endregion

    #region Раскладка control-flow («полётный план»)

    public const double ControlFlowTopPadding = 10;
    public const double ControlFlowBottomPadding = 10;
    public const double ControlFlowSidePadding = 10;
    public const double ControlFlowLegendGap = 4;
    public const double ControlFlowMinGraphWidth = 40;
    public const double ControlFlowMaxReadableBandWidth = 380;
    public const double ControlFlowLegendReserveWidthFraction = 0.30;
    public const double ControlFlowLegendReserveMin = 96;
    public const double ControlFlowLegendReserveMax = 200;

    public const double ControlFlowRefVerticalStep = 34;
    public const double ControlFlowMaxReadableVerticalStep = 40;
    public const double ControlFlowAnchorRadiusBase = 14;
    public const double ControlFlowNodeRadiusBase = 12;
    public const double ControlFlowRadiusScaleMin = 0.4;
    public const double ControlFlowRadiusScaleMax = 1.12;

    /// <summary>Минимальный шаг по Y между уровнями в зависимости от числа уровней (плотность графа).</summary>
    public static double MinVerticalStepForLevelCount(int levelCount) =>
        levelCount switch
        {
            >= 16 => 10,
            >= 12 => 12,
            >= 9 => 14,
            >= 6 => 16,
            _ => 18,
        };

    public const int ControlFlowLabelMaxLength = 22;
    public const int ControlFlowLabelTruncateLength = 19;

    #endregion
}

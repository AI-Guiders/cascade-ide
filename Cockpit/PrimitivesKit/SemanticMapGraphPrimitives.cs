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
    /// <summary>Зазор между правым краем полосы графа и колонкой легенды (DIP).</summary>
    public const double ControlFlowLegendGap = 2;
    public const double ControlFlowMinGraphWidth = 40;
    public const double ControlFlowMaxReadableBandWidth = 380;
    public const double ControlFlowLegendReserveWidthFraction = 0.30;
    public const double ControlFlowLegendReserveMin = 96;
    public const double ControlFlowLegendReserveMax = 200;

    public const double ControlFlowRefVerticalStep = 34;
    /// <summary>Нижняя «комфортная» граница шага; при большой высоте слота фактический шаг может быть выше до <see cref="ControlFlowMaxReadableVerticalStepCap"/>.</summary>
    public const double ControlFlowMaxReadableVerticalStep = 40;
    /// <summary>Верхняя граница шага между уровнями при высоком слоте (не сжимать всё к 40 px, если места много).</summary>
    public const double ControlFlowMaxReadableVerticalStepCap = 88;
    public const double ControlFlowAnchorRadiusBase = 14;
    public const double ControlFlowNodeRadiusBase = 12;
    public const double ControlFlowRadiusScaleMin = 0.4;
    public const double ControlFlowRadiusScaleMax = 1.12;
    /// <summary>Дополнительное сжатие радиуса узлов при узкой читаемой полосе (не растягиваем круги на всю ширину слота).</summary>
    public const double ControlFlowHorizontalRadiusScaleMin = 0.74;

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
    public const int ControlFlowLabelCharBudgetMin = 8;

    /// <summary>
    /// Читаемая ширина полосы графа: верхняя граница — не «простирать» линию на весь широкий viewport;
    /// при узком <paramref name="graphWidth"/> полоса сужается вместе с ним.
    /// </summary>
    public static double ResolveControlFlowReadableBandWidth(double graphWidth) =>
        Math.Clamp(graphWidth, ControlFlowMinGraphWidth, ControlFlowMaxReadableBandWidth);

    /// <summary>
    /// Бюджет символов для боковой подписи call_step (без легенды) от ширины полосы — короче в узком слоте, длиннее при широкой полосе.
    /// </summary>
    public static int ResolveControlFlowLabelCharBudget(double bandWidth)
    {
        // ~7px на символ при Segoe ~11pt; вычитаем запас под круг и отступ.
        var approx = (int)Math.Floor((bandWidth - 52) / 7.0);
        return Math.Clamp(approx, ControlFlowLabelCharBudgetMin, ControlFlowLabelMaxLength);
    }

    /// <summary>
    /// Размер боковой подписи: от вертикального шага и ширины полосы (плотность), без растягивания на весь viewport.
    /// </summary>
    public static double ResolveControlFlowSideLabelFontSize(double bandWidth, double verticalStep)
    {
        var widthFactor = Math.Clamp(bandWidth / 220.0, 0.68, 1.06);
        var stepFactor = Math.Clamp(verticalStep / ControlFlowRefVerticalStep, 0.82, 1.08);
        var combined = SemanticMapRenderInvariants.MinSideLabelFontSize * widthFactor * stepFactor;
        return Math.Clamp(
            combined,
            SemanticMapRenderInvariants.CompactSideLabelFontSizeFloor,
            SemanticMapRenderInvariants.MaxSideLabelFontSize);
    }

    #endregion
}

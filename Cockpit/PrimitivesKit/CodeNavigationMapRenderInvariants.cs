namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Нижние пороги читаемости мини-карты (Glance; карта намерений в UI; ADR 0055 §4).
/// Цвета и стили пера — <see cref="CodeNavigationMapVisualTheme"/>; здесь только размеры/толщины, чтобы не «ломать» глифы при сжатии viewport.
/// </summary>
public static class CodeNavigationMapRenderInvariants
{
    public const double MinGlyphFontSize = 7;
    /// <summary>Базовый размер боковой подписи при нормальной плотности (control flow без легенды).</summary>
    public const double MinSideLabelFontSize = 11;
    /// <summary>Нижний предел при сильном сжатии полосы — не ниже без потери читаемости.</summary>
    public const double CompactSideLabelFontSizeFloor = 9;
    public const double MaxSideLabelFontSize = 12;
    public const double MinLegendCaptionFontSize = 10;
}

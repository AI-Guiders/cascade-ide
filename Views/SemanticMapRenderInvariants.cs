namespace CascadeIDE.Views;

/// <summary>
/// Нижние пороги читаемости мини-карты Semantic Map в режиме Glance (ADR 0055 §4).
/// Цвета и стили пера — <see cref="SemanticMapVisualTheme"/>; здесь только размеры/толщины, чтобы не «ломать» глифы при сжатии viewport.
/// </summary>
internal static class SemanticMapRenderInvariants
{
    public const double MinGlyphFontSize = 7;
    public const double MinSideLabelFontSize = 11;
    public const double MinLegendCaptionFontSize = 10;
}

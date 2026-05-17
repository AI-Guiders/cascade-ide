#nullable enable

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Нижние пороги читаемости graph-backed surface (ADR 0055 §4).</summary>
public static class GraphRenderInvariants
{
    public const double MinGlyphFontSize = 7;
    public const double MinSideLabelFontSize = 11;
    public const double CompactSideLabelFontSizeFloor = 9;
    public const double MaxSideLabelFontSize = 12;
    public const double MinLegendCaptionFontSize = 10;
}

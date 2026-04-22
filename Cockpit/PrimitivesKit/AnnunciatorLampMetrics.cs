using Avalonia;
using Avalonia.Media;
using static CascadeIDE.Cockpit.PrimitivesKit.CockpitPrimitivesPalette.Annunciator;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Размеры и раскладка полосы annunciator (ADR 0063): константы, <see cref="MeasureStrip"/>, фон панели.
/// Отрисовка конкретной ячейки — у типов вроде <see cref="LabeledAnnunciatorLampFace"/>.
/// </summary>
public static class AnnunciatorLampMetrics
{
    public const double DefaultCellWidth = 40;
    public const double DefaultCellHeight = 40;
    public const double DefaultGap = 4;
    public const double DefaultPanelPadding = 6;
    /// <summary>Ламп в одной строке полосы (напр. 4 для готовности окружения).</summary>
    public const int DefaultStripColumns = 4;
    public const double LabelFontSize = 9;
    /// <summary>Две строки подписи в линзе (короткий кегль).</summary>
    public const double TwoLineLabelFontSize = 7.0;

    /// <summary>Размер прямоугольника полосы (с паддингом панели) для заданного числа ячеек.</summary>
    public static Size MeasureStrip(
        int itemCount,
        int columnsPerRow = DefaultStripColumns,
        double cellW = DefaultCellWidth,
        double cellH = DefaultCellHeight,
        double gap = DefaultGap,
        double panelPadding = DefaultPanelPadding)
    {
        if (itemCount <= 0 || columnsPerRow <= 0)
            return new Size(0, 0);

        var rowCount = (itemCount + columnsPerRow - 1) / columnsPerRow;
        var w = panelPadding * 2 + columnsPerRow * cellW + (columnsPerRow - 1) * gap;
        var h = panelPadding * 2 + rowCount * cellH + (rowCount - 1) * gap;
        return new Size(w, h);
    }

    /// <summary>Фон панели под полосой ламп (рамка «корпуса»).</summary>
    public static void DrawPanelBackground(DrawingContext context, Rect bounds) =>
        context.DrawRectangle(new SolidColorBrush(PanelBackground), new Pen(new SolidColorBrush(BezelOuter), 1), bounds);
}

using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.Models;
using static CascadeIDE.Cockpit.PrimitivesKit.CockpitPrimitivesPalette.Annunciator;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Метрики и отрисовка примитива <see cref="CascadeIDE.Cockpit.DeckPrimitiveKind.Lamp"/> в стиле annunciator / Korry
/// (ADR 0063, общий контур <see cref="DrawingContext"/> с ADR 0055).
/// Ячейка — квадрат: объёмная рамка и квадратная линза по центру; <see cref="AnnunciatorLampLevel.Ok"/> = «выключено» (тёмная линза),
/// остальные уровни — подсветка по <see cref="AnnunciatorLampLevel"/> (ADR 0021 §5).
/// </summary>
public static class AnnunciatorLampPrimitives
{
    public const double DefaultCellWidth = 40;
    public const double DefaultCellHeight = 40;
    public const double DefaultGap = 4;
    public const double DefaultPanelPadding = 6;
    /// <summary>Ламп в одной строке полосы (напр. 4 для готовности окружения). Больше ячеек — перенос строки в этом контроле; отдельные группы — вторая полоса ниже, без скролла.</summary>
    public const int DefaultStripColumns = 4;
    public const double LabelFontSize = 9;

    private static readonly Typeface LabelTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

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
    public static void DrawPanelBackground(DrawingContext context, Rect bounds)
    {
        context.DrawRectangle(new SolidColorBrush(PanelBackground), new Pen(new SolidColorBrush(BezelOuter), 1), bounds);
    }

    /// <summary>Одна ячейка: квадратный корпус с фаской, квадратная линза по центру; Ok = тёмная линза (выкл.), иначе цвет уровня.</summary>
    public static void DrawLampCell(DrawingContext context, Rect outerRect, string shortLabel, AnnunciatorLampLevel level)
    {
        context.DrawRectangle(new SolidColorBrush(Housing), new Pen(new SolidColorBrush(BezelInner), 1), outerRect);

        var penHi = new Pen(new SolidColorBrush(BevelHighlight), 1);
        var penLo = new Pen(new SolidColorBrush(BevelShadow), 1);
        context.DrawLine(penHi, outerRect.TopLeft, outerRect.TopRight);
        context.DrawLine(penHi, outerRect.TopLeft, outerRect.BottomLeft);
        context.DrawLine(penLo, outerRect.BottomLeft, outerRect.BottomRight);
        context.DrawLine(penLo, outerRect.TopRight, outerRect.BottomRight);

        var bezel = Deflate(outerRect, 2);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(BezelInner), 1), bezel);

        var inset = 5.0;
        var side = Math.Max(4, Math.Min(outerRect.Width, outerRect.Height) - 2 * inset);
        var lens = new Rect(
            outerRect.X + (outerRect.Width - side) / 2,
            outerRect.Y + (outerRect.Height - side) / 2,
            side,
            side);

        var isOff = level == AnnunciatorLampLevel.Ok;
        if (lens.Width > 1 && lens.Height > 1)
        {
            if (isOff)
            {
                var offBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(OffLensTop, 0),
                        new GradientStop(OffLensBottom, 1),
                    },
                };
                context.DrawRectangle(offBrush, new Pen(new SolidColorBrush(OffLensBorder), 0.75), lens);
            }
            else
            {
                var lampFillColor = LitLens(level);
                var top = Lighten(lampFillColor, 0.2);
                var bottom = Darken(lampFillColor, 0.24);
                var lensBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(top, 0),
                        new GradientStop(lampFillColor, 0.48),
                        new GradientStop(bottom, 1),
                    },
                };
                context.DrawRectangle(lensBrush, new Pen(Brushes.Black, 0.75), lens);

                var hlH = Math.Max(1.5, lens.Height * 0.22);
                var highlight = new Rect(lens.X + 1, lens.Y + 1, lens.Width - 2, hlH);
                context.DrawRectangle(new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)), null, highlight);
            }
        }

        IBrush fillBrush = isOff
            ? new SolidColorBrush(OffLabelFill)
            : Brushes.White;
        var strokeBrush = isOff
            ? new SolidColorBrush(OutlinedTextStrokeDim)
            : new SolidColorBrush(OutlinedTextStrokeLit);

        var ftFill = new FormattedText(
            shortLabel,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            LabelFontSize,
            fillBrush);
        var ftStroke = new FormattedText(
            shortLabel,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            LabelFontSize,
            strokeBrush);

        var origin = new Point(
            outerRect.X + (outerRect.Width - ftFill.Width) / 2,
            outerRect.Y + (outerRect.Height - ftFill.Height) / 2);
        DrawOutlinedText(context, ftStroke, ftFill, origin);
    }

    private static void DrawOutlinedText(DrawingContext context, FormattedText stroke, FormattedText fill, Point origin)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                context.DrawText(stroke, origin + new Vector(dx, dy));
            }
        }

        context.DrawText(fill, origin);
    }

    private static Rect Deflate(Rect r, double d)
    {
        if (r.Width <= 2 * d || r.Height <= 2 * d)
            return new Rect(r.X + d, r.Y + d, 0, 0);
        return new Rect(r.X + d, r.Y + d, r.Width - 2 * d, r.Height - 2 * d);
    }

    private static Color Lighten(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            c.A,
            (byte)Math.Clamp(c.R + (255 - c.R) * amount, 0, 255),
            (byte)Math.Clamp(c.G + (255 - c.G) * amount, 0, 255),
            (byte)Math.Clamp(c.B + (255 - c.B) * amount, 0, 255));
    }

    private static Color Darken(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            c.A,
            (byte)Math.Clamp(c.R * (1 - amount), 0, 255),
            (byte)Math.Clamp(c.G * (1 - amount), 0, 255),
            (byte)Math.Clamp(c.B * (1 - amount), 0, 255));
    }
}

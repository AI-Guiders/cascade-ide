using Avalonia;
using Avalonia.Media;
using static CascadeIDE.Cockpit.PrimitivesKit.CockpitPrimitivesPalette.Annunciator;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Общий корпус Korry / annunciator и утилиты отрисовки для <see cref="CommandArmedStripLampFace"/>,
/// <see cref="WorkspaceSplittersTolLampFace"/>, <see cref="LabeledAnnunciatorLampFace"/> (ADR 0063/0064).
/// </summary>
internal static class AnnunciatorLampChrome
{
    internal static readonly Typeface LabelTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

    internal static void DrawCellHousing(DrawingContext context, Rect outerRect)
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
    }

    /// <summary>Квадратная линза по центру <paramref name="outerRect"/>.</summary>
    internal static Rect ComputeFaceSquare(Rect outerRect, double inset = 5.0)
    {
        var side = Math.Max(4, Math.Min(outerRect.Width, outerRect.Height) - 2 * inset);
        return new Rect(
            outerRect.X + (outerRect.Width - side) / 2,
            outerRect.Y + (outerRect.Height - side) / 2,
            side,
            side);
    }

    internal static void DrawOutlinedText(DrawingContext context, FormattedText stroke, FormattedText fill, Point origin)
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

    internal static Rect Deflate(Rect r, double d)
    {
        if (r.Width <= 2 * d || r.Height <= 2 * d)
            return new Rect(r.X + d, r.Y + d, 0, 0);
        return new Rect(r.X + d, r.Y + d, r.Width - 2 * d, r.Height - 2 * d);
    }

    internal static Color Lighten(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            c.A,
            (byte)Math.Clamp(c.R + (255 - c.R) * amount, 0, 255),
            (byte)Math.Clamp(c.G + (255 - c.G) * amount, 0, 255),
            (byte)Math.Clamp(c.B + (255 - c.B) * amount, 0, 255));
    }

    internal static Color Darken(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            c.A,
            (byte)Math.Clamp(c.R * (1 - amount), 0, 255),
            (byte)Math.Clamp(c.G * (1 - amount), 0, 255),
            (byte)Math.Clamp(c.B * (1 - amount), 0, 255));
    }
}

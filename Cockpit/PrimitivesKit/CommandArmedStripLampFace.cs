using System.Globalization;
using Avalonia;
using Avalonia.Media;
using static CascadeIDE.Cockpit.PrimitivesKit.CockpitPrimitivesPalette.Annunciator;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Лампа «CMD + зелёная полоса» для CascadeChord (ADR 0060): значение — вооружён ли аккорд; отрисовка в стиле annunciator.
/// </summary>
public readonly record struct CommandArmedStripLampFace(bool IsArmed)
{
    /// <summary>IDLE — тёмное лицо; ARMED — подпись CMD и полоса <see cref="CommandArmedStripGreen"/>.</summary>
    public void Draw(DrawingContext context, Rect outerRect)
    {
        AnnunciatorLampChrome.DrawCellHousing(context, outerRect);

        var face = AnnunciatorLampChrome.ComputeFaceSquare(outerRect);

        if (!IsArmed)
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
            context.DrawRectangle(offBrush, new Pen(new SolidColorBrush(OffLensBorder), 0.75), face);
            return;
        }

        var faceBase = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(OffLensTop, 0),
                new GradientStop(OffLensBottom, 1),
            },
        };
        context.DrawRectangle(faceBase, new Pen(new SolidColorBrush(OffLensBorder), 0.75), face);

        var barH = Math.Max(6.0, face.Height * 0.28);
        var padX = Math.Max(2.0, face.Width * 0.08);
        var barY = face.Bottom - barH - 2;
        var barRect = new Rect(face.X + padX, barY, face.Width - 2 * padX, barH);

        var g = CommandArmedStripGreen;
        var gTop = AnnunciatorLampChrome.Lighten(g, 0.18);
        var gBot = AnnunciatorLampChrome.Darken(g, 0.22);
        var barBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(gTop, 0),
                new GradientStop(g, 0.5),
                new GradientStop(gBot, 1),
            },
        };
        context.DrawRectangle(barBrush, new Pen(Brushes.Black, 0.5), barRect);

        var labelBottom = barRect.Y - 3;
        if (labelBottom <= face.Y + 4)
            return;

        var amber = CommandArmedLabelAmber;
        var fillBrush = new SolidColorBrush(amber);
        var strokeBrush = new SolidColorBrush(OutlinedTextStrokeLit);
        const string label = "CMD";
        const double fontSize = 10.5;
        var ftFill = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            AnnunciatorLampChrome.LabelTypeface,
            fontSize,
            fillBrush);
        var ftStroke = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            AnnunciatorLampChrome.LabelTypeface,
            fontSize,
            strokeBrush);
        var origin = new Point(
            face.X + (face.Width - ftFill.Width) / 2,
            face.Y + (labelBottom - face.Y - ftFill.Height) / 2);
        AnnunciatorLampChrome.DrawOutlinedText(context, ftStroke, ftFill, origin);
    }
}

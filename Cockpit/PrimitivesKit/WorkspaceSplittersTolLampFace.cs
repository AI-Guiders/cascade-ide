using System.Globalization;
using Avalonia;
using Avalonia.Media;
using static CascadeIDE.Cockpit.PrimitivesKit.CockpitPrimitivesPalette.Annunciator;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Task cockpit: блокировка сплиттеров (мелодия/бренд TOL; на линзе GND = «на земле» можно двигать границы, IN AIR = зафиксировано) в том же визуальном языке, что <see cref="CommandArmedStripLampFace"/>.
/// </summary>
public readonly record struct WorkspaceSplittersTolLampFace(bool SplittersLocked)
{
    /// <summary>true = IN AIR (заблокировано), false = ON GND (сплиттеры можно двигать).</summary>
    public void Draw(DrawingContext context, Rect outerRect)
    {
        AnnunciatorLampChrome.DrawCellHousing(context, outerRect);

        var face = AnnunciatorLampChrome.ComputeFaceSquare(outerRect);

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

        // Полоса чуть ниже, чем у CMD, чтобы над ней осталась вертикаль на две строки (7pt в две линии не влезали → текст не рисовался).
        var barH = Math.Max(5.0, face.Height * 0.22);
        var padX = Math.Max(2.0, face.Width * 0.08);
        var barY = face.Bottom - barH - 2;
        var barRect = new Rect(face.X + padX, barY, face.Width - 2 * padX, barH);
        var textBandBottomY = barRect.Y - 2;

        if (SplittersLocked)
        {
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
            DrawTolTwoLineLabelInFace(
                context,
                face,
                textBandBottomY,
                "IN",
                "AIR",
                Brushes.White,
                new SolidColorBrush(OutlinedTextStrokeLit));
        }
        else
        {
            var r = Color.Parse("#C62828");
            var rTop = AnnunciatorLampChrome.Lighten(r, 0.12);
            var rBot = AnnunciatorLampChrome.Darken(r, 0.2);
            var barBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(rTop, 0),
                    new GradientStop(r, 0.5),
                    new GradientStop(rBot, 1),
                },
            };
            context.DrawRectangle(barBrush, new Pen(Brushes.Black, 0.5), barRect);
            var amber = new SolidColorBrush(CommandArmedLabelAmber);
            var stroke = new SolidColorBrush(OutlinedTextStrokeLit);
            DrawTolTwoLineLabelInFace(
                context,
                face,
                textBandBottomY,
                "ON",
                "GND",
                amber,
                stroke);
        }
    }

    /// <summary>Кегль подбирается по вертикали, чтобы в узкой зоне над полосой текст не исчезал целиком.</summary>
    private static void DrawTolTwoLineLabelInFace(
        DrawingContext context,
        Rect face,
        double labelBottomY,
        string line1,
        string line2,
        IBrush fillBrush,
        IBrush strokeBrush)
    {
        if (labelBottomY <= face.Y + 2)
            return;

        const double lineGap = 0.4;
        var bandTop = face.Y + 2;
        var available = labelBottomY - bandTop;
        if (available <= 0)
            return;

        ReadOnlySpan<double> trySizes = [5.5, 5.0, 4.5, 4.0, 3.5];
        foreach (var fontSize in trySizes)
        {
            var (ft1Fill, ft1Stroke, ft2Fill, ft2Stroke) = BuildTolLineFormatted(
                line1, line2, fontSize, fillBrush, strokeBrush);
            var totalH = ft1Fill.Height + lineGap + ft2Fill.Height;
            if (totalH > available)
                continue;

            var y = bandTop + (available - totalH) / 2;
            var x1 = face.X + (face.Width - ft1Fill.Width) / 2;
            AnnunciatorLampChrome.DrawOutlinedText(context, ft1Stroke, ft1Fill, new Point(x1, y));
            y += ft1Fill.Height + lineGap;
            var x2 = face.X + (face.Width - ft2Fill.Width) / 2;
            AnnunciatorLampChrome.DrawOutlinedText(context, ft2Stroke, ft2Fill, new Point(x2, y));
            return;
        }
    }

    private static (FormattedText Ft1Fill, FormattedText Ft1Stroke, FormattedText Ft2Fill, FormattedText Ft2Stroke) BuildTolLineFormatted(
        string line1,
        string line2,
        double fontSize,
        IBrush fillBrush,
        IBrush strokeBrush)
    {
        var ft1Fill = new FormattedText(
            line1,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            AnnunciatorLampChrome.LabelTypeface,
            fontSize,
            fillBrush);
        var ft1Stroke = new FormattedText(
            line1,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            AnnunciatorLampChrome.LabelTypeface,
            fontSize,
            strokeBrush);
        var ft2Fill = new FormattedText(
            line2,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            AnnunciatorLampChrome.LabelTypeface,
            fontSize,
            fillBrush);
        var ft2Stroke = new FormattedText(
            line2,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            AnnunciatorLampChrome.LabelTypeface,
            fontSize,
            strokeBrush);
        return (ft1Fill, ft1Stroke, ft2Fill, ft2Stroke);
    }
}

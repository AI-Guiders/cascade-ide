using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.Models;
using static CascadeIDE.Cockpit.PrimitivesKit.CockpitPrimitivesPalette.Annunciator;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Стандартная ячейка annunciator: подпись и уровень <see cref="AnnunciatorLampLevel"/> (ADR 0021 §5, ADR 0063).
/// </summary>
public readonly record struct LabeledAnnunciatorLampFace(string ShortLabel, AnnunciatorLampLevel Level)
{
    public void Draw(DrawingContext context, Rect outerRect)
    {
        AnnunciatorLampChrome.DrawCellHousing(context, outerRect);

        var lens = AnnunciatorLampChrome.ComputeFaceSquare(outerRect);

        var isOff = Level == AnnunciatorLampLevel.Ok;
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
                var lampFillColor = LitLens(Level);
                var top = AnnunciatorLampChrome.Lighten(lampFillColor, 0.2);
                var bottom = AnnunciatorLampChrome.Darken(lampFillColor, 0.24);
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

        DrawLampShortLabel(context, outerRect, ShortLabel, fillBrush, strokeBrush);
    }

    private static void DrawLampShortLabel(
        DrawingContext context,
        Rect outerRect,
        string shortLabel,
        IBrush fillBrush,
        IBrush strokeBrush)
    {
        var rawLines = shortLabel.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var lines = rawLines.Length <= 2 ? rawLines : rawLines.Take(2).ToArray();
        if (lines.Length == 0)
            return;

        if (lines.Length == 1)
        {
            var line = lines[0];
            var ftFill = new FormattedText(
                line,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                AnnunciatorLampChrome.LabelTypeface,
                AnnunciatorLampMetrics.LabelFontSize,
                fillBrush);
            var ftStroke = new FormattedText(
                line,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                AnnunciatorLampChrome.LabelTypeface,
                AnnunciatorLampMetrics.LabelFontSize,
                strokeBrush);
            var origin = new Point(
                outerRect.X + (outerRect.Width - ftFill.Width) / 2,
                outerRect.Y + (outerRect.Height - ftFill.Height) / 2);
            AnnunciatorLampChrome.DrawOutlinedText(context, ftStroke, ftFill, origin);
            return;
        }

        const double lineGap = 0.5;
        var fontSize = AnnunciatorLampMetrics.TwoLineLabelFontSize;
        var formatted = new (FormattedText Fill, FormattedText Stroke)[2];
        double totalH = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var fFill = new FormattedText(
                line,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                AnnunciatorLampChrome.LabelTypeface,
                fontSize,
                fillBrush);
            var fStroke = new FormattedText(
                line,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                AnnunciatorLampChrome.LabelTypeface,
                fontSize,
                strokeBrush);
            formatted[i] = (fFill, fStroke);
            totalH += fFill.Height;
        }

        totalH += lineGap;
        var y = outerRect.Y + (outerRect.Height - totalH) / 2;
        for (var i = 0; i < lines.Length; i++)
        {
            var (ftFill, ftStroke) = formatted[i];
            var x = outerRect.X + (outerRect.Width - ftFill.Width) / 2;
            AnnunciatorLampChrome.DrawOutlinedText(context, ftStroke, ftFill, new Point(x, y));
            y += ftFill.Height + (i == 0 ? lineGap : 0);
        }
    }
}

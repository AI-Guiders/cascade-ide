using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CascadeIDE.Views.UiKit;

/// <summary>
/// Minimal ECAM-like dial gauge: arc + ticks + needle.
/// Text/digital readouts are composed outside (see reference image).
/// </summary>
public sealed class EcamDialGauge : Control
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<EcamDialGauge, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<EcamDialGauge, double>(nameof(Maximum), 1);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<EcamDialGauge, double>(nameof(Value), 0);

    /// <summary>Angle where arc starts (degrees). Default matches ECAM-ish top arc.</summary>
    public static readonly StyledProperty<double> StartAngleDegProperty =
        AvaloniaProperty.Register<EcamDialGauge, double>(nameof(StartAngleDeg), -210);

    /// <summary>Arc sweep (degrees).</summary>
    public static readonly StyledProperty<double> SweepAngleDegProperty =
        AvaloniaProperty.Register<EcamDialGauge, double>(nameof(SweepAngleDeg), 240);

    public static readonly StyledProperty<int> TickCountProperty =
        AvaloniaProperty.Register<EcamDialGauge, int>(nameof(TickCount), 6);

    public static readonly StyledProperty<IBrush> ArcBrushProperty =
        AvaloniaProperty.Register<EcamDialGauge, IBrush>(nameof(ArcBrush), Brushes.LightGray);

    public static readonly StyledProperty<IBrush> TickBrushProperty =
        AvaloniaProperty.Register<EcamDialGauge, IBrush>(nameof(TickBrush), Brushes.LightGray);

    public static readonly StyledProperty<IBrush> NeedleBrushProperty =
        AvaloniaProperty.Register<EcamDialGauge, IBrush>(nameof(NeedleBrush), new SolidColorBrush(Color.Parse("#00FF6A")));

    public static readonly StyledProperty<IBrush> LimitBrushProperty =
        AvaloniaProperty.Register<EcamDialGauge, IBrush>(nameof(LimitBrush), new SolidColorBrush(Color.Parse("#FFB300")));

    /// <summary>Optional limit marker value (draws a small orange tick on the arc).</summary>
    public static readonly StyledProperty<double?> LimitMarkerValueProperty =
        AvaloniaProperty.Register<EcamDialGauge, double?>(nameof(LimitMarkerValue));

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double StartAngleDeg { get => GetValue(StartAngleDegProperty); set => SetValue(StartAngleDegProperty, value); }
    public double SweepAngleDeg { get => GetValue(SweepAngleDegProperty); set => SetValue(SweepAngleDegProperty, value); }
    public int TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }
    public IBrush ArcBrush { get => GetValue(ArcBrushProperty); set => SetValue(ArcBrushProperty, value); }
    public IBrush TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
    public IBrush NeedleBrush { get => GetValue(NeedleBrushProperty); set => SetValue(NeedleBrushProperty, value); }
    public IBrush LimitBrush { get => GetValue(LimitBrushProperty); set => SetValue(LimitBrushProperty, value); }
    public double? LimitMarkerValue { get => GetValue(LimitMarkerValueProperty); set => SetValue(LimitMarkerValueProperty, value); }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
            return;

        var stroke = Math.Max(1, Math.Min(bounds.Width, bounds.Height) * 0.03);
        var tick = Math.Max(1, stroke * 0.9);

        var size = Math.Min(bounds.Width, bounds.Height);
        var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        var radius = size * 0.42;

        var arcPen = new Pen(ArcBrush, stroke, lineCap: PenLineCap.Round);
        var tickPen = new Pen(TickBrush, tick, lineCap: PenLineCap.Round);
        var needlePen = new Pen(NeedleBrush, stroke * 1.15, lineCap: PenLineCap.Round);

        // Arc
        var start = StartAngleDeg;
        var end = StartAngleDeg + SweepAngleDeg;
        context.DrawGeometry(null, arcPen, BuildArc(center, radius, start, end));

        // Ticks
        var ticks = Math.Max(2, TickCount);
        for (var i = 0; i < ticks; i++)
        {
            var t = ticks == 1 ? 0 : (double)i / (ticks - 1);
            var a = Lerp(start, end, t);
            var p0 = Polar(center, radius * 0.92, a);
            var p1 = Polar(center, radius * 1.02, a);
            context.DrawLine(tickPen, p0, p1);
        }

        // Optional limit marker
        if (LimitMarkerValue is { } lim && Maximum > Minimum)
        {
            var lt = Math.Clamp((lim - Minimum) / (Maximum - Minimum), 0, 1);
            var a = Lerp(start, end, lt);
            var p0 = Polar(center, radius * 0.88, a);
            var p1 = Polar(center, radius * 1.08, a);
            var limPen = new Pen(LimitBrush, tick * 1.2, lineCap: PenLineCap.Round);
            context.DrawLine(limPen, p0, p1);
        }

        // Needle
        var v = Value;
        var max = Maximum;
        var min = Minimum;
        var vt = max > min ? Math.Clamp((v - min) / (max - min), 0, 1) : 0;
        var va = Lerp(start, end, vt);
        var needleEnd = Polar(center, radius * 0.95, va);
        context.DrawLine(needlePen, center, needleEnd);

        // Small hub
        context.DrawEllipse(NeedleBrush, null, center, stroke * 0.55, stroke * 0.55);
    }

    private static Geometry BuildArc(Point c, double r, double startDeg, double endDeg)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        var p0 = Polar(c, r, startDeg);
        var p1 = Polar(c, r, endDeg);
        var sweep = Math.Abs(endDeg - startDeg);
        var isLarge = sweep >= 180;
        ctx.BeginFigure(p0, isFilled: false);
        ctx.ArcTo(
            p1,
            new Size(r, r),
            rotationAngle: 0,
            isLargeArc: isLarge,
            sweepDirection: endDeg >= startDeg ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
        ctx.EndFigure(false);
        return g;
    }

    private static Point Polar(Point c, double r, double deg)
    {
        var rad = deg * (Math.PI / 180.0);
        return new Point(c.X + r * Math.Cos(rad), c.Y + r * Math.Sin(rad));
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}


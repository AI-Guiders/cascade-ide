using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services;

public sealed record EditorDiagnosticStrip(
    int Start,
    int Length,
    DiagnosticSeverity Severity,
    string Id,
    string Message,
    int Line1,
    int Column1);

public sealed class EditorDiagnosticBackgroundRenderer(Func<IReadOnlyList<EditorDiagnosticStrip>> getStrips) : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null)
            return;

        var errPen = new Pen(new SolidColorBrush(Color.FromRgb(220, 70, 70)), 1.25);
        var warnPen = new Pen(new SolidColorBrush(Color.FromRgb(190, 150, 40)), 1.05);
        foreach (var strip in getStrips())
        {
            var pen = strip.Severity == DiagnosticSeverity.Error ? errPen : warnPen;
            var seg = new DiagnosticTextSegment(strip.Start, strip.Length);
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
                DrawWavyUnderline(drawingContext, pen, rect.Left, rect.Bottom - 0.5, rect.Width);
        }
    }

    private static void DrawWavyUnderline(DrawingContext dc, IPen pen, double x, double y, double width)
    {
        if (width < 0.5)
            return;
        const double step = 2.8;
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(new Point(x, y), false);
            var px = x;
            var flip = false;
            while (px < x + width)
            {
                var nx = Math.Min(px + step, x + width);
                var dy = flip ? 1.25 : -1.25;
                g.LineTo(new Point(nx, y + dy));
                px = nx;
                flip = !flip;
            }
        }

        dc.DrawGeometry(null, pen, geo);
    }

    private sealed class DiagnosticTextSegment(int offset, int length) : AvaloniaEdit.Document.ISegment
    {
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        int AvaloniaEdit.Document.ISegment.EndOffset => Offset + Length;
    }
}

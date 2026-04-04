using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Views;

internal sealed class BreakpointLineRenderer(Func<IReadOnlyList<int>> getBreakpointLines) : IBackgroundRenderer
{
    private const double SymbolRadius = 5;
    private static readonly SolidColorBrush s_backBrush = new(Color.FromArgb(40, 200, 80, 80));
    private static readonly SolidColorBrush s_symbolBrush = new(Color.FromRgb(200, 80, 80));
    private static readonly Pen s_symbolPen = new(new SolidColorBrush(Color.FromRgb(160, 60, 60)), 1);

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var document = textView.Document;
        if (document is null) return;
        var lines = getBreakpointLines();
        if (lines.Count == 0) return;
        foreach (var lineNumber in lines)
        {
            if (lineNumber < 1 || lineNumber > document.LineCount) continue;
            var line = document.GetLineByNumber(lineNumber);
            var first = true;
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
            {
                drawingContext.DrawRectangle(s_backBrush, null, rect);
                if (first)
                {
                    var centerX = rect.Left + SymbolRadius + 2;
                    var centerY = rect.Top + rect.Height / 2;
                    drawingContext.DrawEllipse(s_symbolBrush, s_symbolPen, new Rect(centerX - SymbolRadius, centerY - SymbolRadius, SymbolRadius * 2, SymbolRadius * 2));
                    first = false;
                }
            }
        }
    }
}

internal sealed class DebugCurrentLineRenderer(Func<int> getCurrentLine) : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var lineNumber = getCurrentLine();
        if (lineNumber < 1) return;
        var document = textView.Document;
        if (document is null || lineNumber > document.LineCount) return;
        var line = document.GetLineByNumber(lineNumber);
        var brush = new SolidColorBrush(Color.FromArgb(60, 255, 200, 80));
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
            drawingContext.DrawRectangle(brush, null, rect);
    }
}

/// <summary>Стрелка текущей инструкции в духе VS (жёлтый треугольник у левого края строки).</summary>
internal sealed class DebugInstructionArrowRenderer(Func<int> getCurrentLine) : IBackgroundRenderer
{
    private static readonly SolidColorBrush s_fill = new(Color.FromRgb(255, 215, 48));
    private static readonly Pen s_stroke = new(new SolidColorBrush(Color.FromRgb(160, 120, 0)), 0.85);

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var lineNumber = getCurrentLine();
        if (lineNumber < 1) return;
        var document = textView.Document;
        if (document is null || lineNumber > document.LineCount) return;
        var line = document.GetLineByNumber(lineNumber);
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
        {
            var h = Math.Min(14, Math.Max(6, rect.Height - 2));
            var w = h * 0.55;
            var left = rect.Left + 1.5;
            var top = rect.Top + (rect.Height - h) / 2;
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(new Point(left, top), true);
                g.LineTo(new Point(left + w, top + h * 0.5));
                g.LineTo(new Point(left, top + h));
            }

            drawingContext.DrawGeometry(s_fill, s_stroke, geo);
        }
    }
}

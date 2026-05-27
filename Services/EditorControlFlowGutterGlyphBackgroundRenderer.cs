using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Services;

/// <summary>Gutter-глифы control-flow (совпадают с мини-картой Skia / HUD).</summary>
public sealed class EditorControlFlowGutterGlyphBackgroundRenderer(
    Func<string?> getFilePath,
    Func<string?, IReadOnlyList<ControlFlowLineVisual>?> getLineVisuals,
    Func<string?, bool> isVirtualSpacingActive) : IBackgroundRenderer
{
    private const double R = EditorControlFlowVirtualSpacing.GlyphRadius;
    private const double LeftPad = EditorControlFlowVirtualSpacing.LanePadding;
    private static readonly IBrush s_glyph = new SolidColorBrush(Color.FromRgb(180, 188, 204));
    private static readonly IBrush s_anchorFill = new SolidColorBrush(Color.FromArgb(95, 64, 140, 200));
    private static readonly Pen s_stroke = new(new SolidColorBrush(Color.FromRgb(120, 132, 156)), 1);
    private static readonly Pen s_arrow = new(new SolidColorBrush(Color.FromRgb(165, 178, 210)), 1.15)
    {
        LineCap = PenLineCap.Round
    };

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var document = textView.Document;
        if (document is null || document.LineCount == 0 || !textView.VisualLinesValid)
            return;

        IReadOnlyList<ControlFlowLineVisual>? visuals;
        try
        {
            visuals = getLineVisuals(getFilePath());
        }
        catch
        {
            return;
        }

        if (visuals is null || visuals.Count == 0)
            return;

        var filePath = getFilePath();
        var useVirtualLane = isVirtualSpacingActive(filePath);

        var rawSize = TextElement.GetFontSize(textView);
        if (rawSize <= 0 || double.IsNaN(rawSize))
            rawSize = 13.0;
        var glyphFont = Math.Clamp(rawSize * 0.68, 7.8, 10.8);
        var family = TextElement.GetFontFamily(textView) ?? FontFamily.Default;
        var typeface = new Typeface(
            family,
            TextElement.GetFontStyle(textView),
            FontWeight.SemiBold);

        foreach (var v in visuals)
        {
            if (v.LineOneBased < 1 || v.LineOneBased > document.LineCount)
                continue;
            var line = document.GetLineByNumber(v.LineOneBased);
            if (!TryGetLineCenter(textView, v.LineOneBased, useVirtualLane, out var cx, out var cy))
                continue;

            switch (v.VisualKind)
            {
                case ControlFlowNodeVisualKind.Anchor:
                    drawingContext.DrawEllipse(s_anchorFill, s_stroke, new Rect(cx - R, cy - R, R * 2, R * 2));
                    break;
                case ControlFlowNodeVisualKind.Diamond:
                    DrawDiamond(drawingContext, cx, cy, R, s_stroke);
                    break;
                case ControlFlowNodeVisualKind.Exit:
                    drawingContext.DrawEllipse(null, s_stroke, new Rect(cx - R, cy - R, R * 2, R * 2));
                    if (v.ShowExitArrow)
                        DrawNeArrow(drawingContext, cx, cy, R * 0.85);
                    continue;
                case ControlFlowNodeVisualKind.Circle:
                    drawingContext.DrawEllipse(null, s_stroke, new Rect(cx - R, cy - R, R * 2, R * 2));
                    break;
            }

            if (!string.IsNullOrEmpty(v.TextGlyph))
            {
                var brush = v.VisualKind == ControlFlowNodeVisualKind.Anchor
                    ? Brushes.White
                    : s_glyph;
                var ft = new FormattedText(
                    v.TextGlyph,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    glyphFont,
                    brush);
                drawingContext.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
            }
        }
    }

    private static void DrawNeArrow(DrawingContext ctx, double cx, double cy, double len)
    {
        var dirX = 0.70710678118654757;
        var dirY = -0.70710678118654757;
        var tip = new Point(cx + dirX * (len * 0.35), cy + dirY * (len * 0.35));
        var basePt = new Point(tip.X - dirX * len, tip.Y - dirY * len);
        ctx.DrawLine(s_arrow, basePt, tip);
        var wing = len * 0.38;
        var px = -dirY;
        var py = dirX;
        ctx.DrawLine(
            s_arrow,
            new Point(tip.X - dirX * wing + px * wing * 0.35, tip.Y - dirY * wing + py * wing * 0.35),
            tip);
        ctx.DrawLine(
            s_arrow,
            new Point(tip.X - dirX * wing - px * wing * 0.35, tip.Y - dirY * wing - py * wing * 0.35),
            tip);
    }

    private static void DrawDiamond(DrawingContext ctx, double cx, double cy, double r, Pen pen)
    {
        var geo = new StreamGeometry();
        using (var ig = geo.Open())
        {
            ig.BeginFigure(new Point(cx, cy - r), true);
            ig.LineTo(new Point(cx + r, cy));
            ig.LineTo(new Point(cx, cy + r));
            ig.LineTo(new Point(cx - r, cy));
            ig.EndFigure(true);
        }

        ctx.DrawGeometry(null, pen, geo);
    }

    private static bool TryGetLineCenter(
        TextView textView,
        int lineOneBased,
        bool useVirtualLane,
        out double cx,
        out double cy)
    {
        cx = 0;
        cy = 0;
        if (lineOneBased < 1 || lineOneBased > textView.Document!.LineCount)
            return false;

        var line = textView.Document.GetLineByNumber(lineOneBased);
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
        {
            cy = rect.Top + rect.Height / 2;
            cx = useVirtualLane
                ? ResolveVirtualLaneCenterX(rect)
                : rect.Left + LeftPad + R;
            return true;
        }

        return false;
    }

    /// <summary>Центр virtual-spacing сегмента или слева от текста на переносе строки.</summary>
    private static double ResolveVirtualLaneCenterX(Rect rect)
    {
        if (rect.Width <= EditorControlFlowVirtualSpacing.LaneWidthPixels * 1.25 + 1)
            return rect.Left + rect.Width / 2;

        return rect.Left - EditorControlFlowVirtualSpacing.LaneHalfWidth;
    }
}

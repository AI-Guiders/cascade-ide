using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Services;

/// <summary>
/// Отрисовка узлов control-flow в редакторе — те же заливки/обводки, что <see cref="Views.SkiaKit.Graph.SkiaGraphSceneDrawing"/> (палитра CFG).
/// </summary>
public static class ControlFlowEditorNodePainter
{
    private const string ExitStepKind = "exit_step";
    private const string ConditionStepKind = "condition_step";
    private const string HandlerStepKind = "handler_step";

    private static readonly IBrush AnchorFill = Brush(CockpitPrimitivesPalette.CodeNavigationMap.AnchorFill);
    private static readonly IBrush ConditionFill = Brush(CockpitPrimitivesPalette.CodeNavigationMap.ConditionFill);
    private static readonly IBrush ExitFill = Brush(CockpitPrimitivesPalette.CodeNavigationMap.ExitFill);
    private static readonly IBrush CallFill = Brush(CockpitPrimitivesPalette.CodeNavigationMap.CallFill);
    private static readonly IBrush HandlerFill = Brush(CockpitPrimitivesPalette.CodeNavigationMap.HandlerFill);
    private static readonly Pen NodeStroke = new(new SolidColorBrush(CockpitPrimitivesPalette.CodeNavigationMap.NodeStroke), 1);
    private static readonly IBrush GlyphOnFill = Brushes.White;
    private static readonly IBrush GlyphOnExit = new SolidColorBrush(Color.FromRgb(220, 228, 240));

    public static IBrush ResolveFillBrush(string nodeKind, ControlFlowNodeVisualKind visualKind)
    {
        if (visualKind == ControlFlowNodeVisualKind.Anchor)
            return AnchorFill;
        if (visualKind == ControlFlowNodeVisualKind.Diamond
            || IsConditionKind(nodeKind))
            return ConditionFill;
        if (visualKind == ControlFlowNodeVisualKind.Exit || IsExitKind(nodeKind))
            return ExitFill;
        if (IsHandlerKind(nodeKind))
            return HandlerFill;
        return CallFill;
    }

    public static IBrush ResolveGlyphBrush(ControlFlowNodeVisualKind visualKind) =>
        visualKind == ControlFlowNodeVisualKind.Exit ? GlyphOnExit : GlyphOnFill;

    public static Pen NodeStrokePen => NodeStroke;

    public static IBrush NodeStrokeBrush => NodeStroke.Brush ?? Brushes.Black;

    public static void DrawNode(
        DrawingContext ctx,
        ControlFlowLineVisual visual,
        double centerX,
        double centerY,
        double radius,
        Typeface typeface,
        double glyphFontSize)
    {
        var fill = ResolveFillBrush(visual.NodeKind, visual.VisualKind);
        switch (visual.VisualKind)
        {
            case ControlFlowNodeVisualKind.Anchor:
                ctx.DrawEllipse(fill, NodeStroke, new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2));
                break;
            case ControlFlowNodeVisualKind.Diamond:
                DrawDiamond(ctx, centerX, centerY, radius, fill, NodeStroke);
                break;
            case ControlFlowNodeVisualKind.Exit:
                ctx.DrawEllipse(fill, NodeStroke, new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2));
                if (visual.ShowExitArrow)
                    DrawNeArrow(ctx, centerX, centerY, radius * 0.85);
                DrawGlyphIfAny(ctx, visual, centerX, centerY, typeface, glyphFontSize);
                return;
            case ControlFlowNodeVisualKind.Circle:
                ctx.DrawEllipse(fill, NodeStroke, new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2));
                break;
        }

        DrawGlyphIfAny(ctx, visual, centerX, centerY, typeface, glyphFontSize);
    }

    private static void DrawGlyphIfAny(
        DrawingContext ctx,
        ControlFlowLineVisual visual,
        double cx,
        double cy,
        Typeface typeface,
        double fontSize)
    {
        if (string.IsNullOrEmpty(visual.TextGlyph))
            return;

        var ft = new FormattedText(
            visual.TextGlyph,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            ResolveGlyphBrush(visual.VisualKind));
        ctx.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
    }

    private static void DrawDiamond(DrawingContext ctx, double cx, double cy, double r, IBrush fill, Pen pen)
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

        ctx.DrawGeometry(fill, pen, geo);
    }

    private static void DrawNeArrow(DrawingContext ctx, double cx, double cy, double len)
    {
        var pen = new Pen(GlyphOnExit, 1.15) { LineCap = PenLineCap.Round };
        const double dirX = 0.70710678118654757;
        const double dirY = -0.70710678118654757;
        var tip = new Point(cx + dirX * (len * 0.35), cy + dirY * (len * 0.35));
        var basePt = new Point(tip.X - dirX * len, tip.Y - dirY * len);
        ctx.DrawLine(pen, basePt, tip);
        var wing = len * 0.38;
        var px = -dirY;
        var py = dirX;
        ctx.DrawLine(
            pen,
            new Point(tip.X - dirX * wing + px * wing * 0.35, tip.Y - dirY * wing + py * wing * 0.35),
            tip);
        ctx.DrawLine(
            pen,
            new Point(tip.X - dirX * wing - px * wing * 0.35, tip.Y - dirY * wing - py * wing * 0.35),
            tip);
    }

    private static bool IsExitKind(string kind) =>
        string.Equals(kind, ExitStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionKind(string kind) =>
        string.Equals(kind, ConditionStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsHandlerKind(string kind) =>
        string.Equals(kind, HandlerStepKind, StringComparison.OrdinalIgnoreCase);

    private static SolidColorBrush Brush(Color c) => new(c);
}

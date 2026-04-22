using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.PrimitivesKit;

public static partial class SemanticMapSceneDrawing
{
    private static void DrawNodes(DrawingContext context, CodeNavigationMapGraphSceneVm scene, SemanticMapVisualTheme theme)
    {
        var useLegend = scene.UseLegendColumn;
        foreach (var n in scene.Nodes)
        {
            var highlighted = scene.HighlightedNodeIds.Contains(n.Id);
            if (n.Shape == CodeNavigationMapNodeShape.Condition)
                DrawConditionBranch(context, theme, n, highlighted);
            else
            {
                context.DrawEllipse(ResolveNodeFill(theme, n), theme.NodeStrokePen, n.Center, n.Radius, n.Radius);
                if (highlighted)
                    context.DrawEllipse(null, theme.HighlightedNodePen, n.Center, n.Radius + 3, n.Radius + 3);
            }

            var glyph = BuildNodeGlyph(n, useLegend);
            if (IsExitNode(n))
            {
                var arrowLen = Math.Clamp(n.Radius * 0.95, 4.5, 12);
                DrawNorthEastExitArrowShaftCentered(context, theme.GlyphBrush, n.Center, arrowLen, 1.15);
            }
            else if (!string.IsNullOrEmpty(glyph))
            {
                var maxGlyph = Math.Max(SemanticMapRenderInvariants.MinGlyphFontSize, n.Radius * 0.92);
                var fontSize = n.LegendIndex is > 99
                    ? SemanticMapRenderInvariants.MinGlyphFontSize
                    : Math.Clamp(maxGlyph, SemanticMapRenderInvariants.MinGlyphFontSize, 11);
                var glyphText = new FormattedText(
                    glyph,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    theme.GlyphTypeface,
                    fontSize,
                    theme.GlyphBrush);
                var glyphOrigin = new Point(
                    n.Center.X - glyphText.Width / 2,
                    n.Center.Y - glyphText.Height / 2);
                context.DrawText(glyphText, glyphOrigin);
            }

            var fullLabel = BuildNodeFullLabel(n, useLegend);
            if (!string.IsNullOrWhiteSpace(fullLabel))
            {
                var sideFont = scene.SideLabelFontSizePx ?? SemanticMapRenderInvariants.MinSideLabelFontSize;
                var labelText = new FormattedText(
                    fullLabel,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    theme.SideLabelTypeface,
                    sideFont,
                    theme.SideLabelBrush);
                var labelOrigin = new Point(
                    n.Center.X + n.Radius + 6,
                    n.Center.Y - labelText.Height / 2);
                context.DrawText(labelText, labelOrigin);
            }
        }
    }

    /// <summary>Узел условия в control-flow: заливка и обводка ромба (классический знак ветвления).</summary>
    private static void DrawConditionBranch(DrawingContext context, SemanticMapVisualTheme theme, CodeNavigationMapGraphNodeLayout n, bool highlighted)
    {
        var geo = new StreamGeometry();
        var c = n.Center;
        var r = n.Radius;
        using (var ig = geo.Open())
        {
            ig.BeginFigure(new Point(c.X, c.Y - r), true);
            ig.LineTo(new Point(c.X + r, c.Y));
            ig.LineTo(new Point(c.X, c.Y + r));
            ig.LineTo(new Point(c.X - r, c.Y));
            ig.EndFigure(true);
        }

        context.DrawGeometry(ResolveNodeFill(theme, n), theme.NodeStrokePen, geo);
        if (highlighted)
        {
            var hr = r + 3;
            var hgeo = new StreamGeometry();
            using (var ig = hgeo.Open())
            {
                ig.BeginFigure(new Point(c.X, c.Y - hr), true);
                ig.LineTo(new Point(c.X + hr, c.Y));
                ig.LineTo(new Point(c.X, c.Y + hr));
                ig.LineTo(new Point(c.X - hr, c.Y));
                ig.EndFigure(true);
            }

            context.DrawGeometry(null, theme.HighlightedNodePen, hgeo);
        }
    }

    private static IBrush ResolveNodeFill(SemanticMapVisualTheme theme, CodeNavigationMapGraphNodeLayout node)
    {
        if (node.IsAnchor)
            return theme.AnchorFill;
        if (IsConditionNode(node))
            return theme.ConditionFill;
        if (IsExitNode(node))
            return theme.ExitFill;
        if (IsHandlerNode(node))
            return theme.HandlerFill;
        return theme.CallFill;
    }

    private static string BuildNodeGlyph(CodeNavigationMapGraphNodeLayout node, bool useLegendColumn)
    {
        if (node.IsAnchor)
            return "A";
        if (string.Equals(node.Kind, "protected_step", StringComparison.OrdinalIgnoreCase))
            return "T";
        if (IsExitNode(node))
            return "";
        if (node.LegendIndex is { } idx && useLegendColumn)
            return idx.ToString(CultureInfo.InvariantCulture);
        if (IsConditionNode(node))
            return "?";
        if (IsHandlerNode(node))
            return "!";
        return "•";
    }

    private static string? BuildNodeFullLabel(CodeNavigationMapGraphNodeLayout node, bool useLegendColumn)
    {
        if (useLegendColumn)
            return null;
        if (node.IsAnchor || IsConditionNode(node) || IsExitNode(node))
            return null;
        var label = node.Label?.Trim();
        if (string.IsNullOrWhiteSpace(label))
            return null;
        return label;
    }

    private static bool IsExitNode(CodeNavigationMapGraphNodeLayout node) =>
        string.Equals(node.Kind, ExitStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionNode(CodeNavigationMapGraphNodeLayout node) =>
        string.Equals(node.Kind, ConditionStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsHandlerNode(CodeNavigationMapGraphNodeLayout node) =>
        string.Equals(node.Kind, "handler_step", StringComparison.OrdinalIgnoreCase);
}

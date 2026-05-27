using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CascadeIDE.Cockpit.Graph.Layout;

namespace CascadeIDE.Views.SkiaKit.Graph;

public static partial class SkiaGraphSceneDrawing
{
    private static void DrawNodes(DrawingContext context, GraphLayoutScene scene, SkiaGraphVisualTheme theme)
    {
        var useLegend = scene.UseLegendColumn;
        foreach (var n in scene.Nodes)
        {
            var highlighted = scene.HighlightedNodeIds.Contains(n.Id);
            if (n.Shape == GraphNodeShape.Condition)
                DrawConditionBranch(context, theme, n, highlighted);
            else
            {
                context.DrawEllipse(ResolveNodeFill(theme, n), theme.NodeStrokePen, n.Center, n.Radius, n.Radius);
                if (highlighted)
                    context.DrawEllipse(null, theme.HighlightedNodePen, n.Center, n.Radius + 3, n.Radius + 3);
            }

            var glyph = BuildNodeGlyph(n, useLegend, scene.ShowNodeLegendGlyphs);
            if (IsExitNode(n))
            {
                var arrowLen = Math.Clamp(n.Radius * 0.95, 4.5, 12);
                DrawNorthEastExitArrowShaftCentered(context, theme.GlyphBrush, n.Center, arrowLen, 1.15);
            }
            else if (!string.IsNullOrEmpty(glyph))
            {
                var maxGlyph = Math.Max(SkiaGraphRenderInvariants.MinGlyphFontSize, n.Radius * 0.92);
                var fontSize = n.LegendIndex is > 99
                    ? SkiaGraphRenderInvariants.MinGlyphFontSize
                    : Math.Clamp(maxGlyph, SkiaGraphRenderInvariants.MinGlyphFontSize, 11);
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

            var fullLabel = BuildNodeFullLabel(n, useLegend, scene);
            if (!string.IsNullOrWhiteSpace(fullLabel))
            {
                var sideFont = scene.SideLabelFontSizePx ?? SkiaGraphRenderInvariants.MinSideLabelFontSize;
                var labelText = new FormattedText(
                    fullLabel,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    theme.SideLabelTypeface,
                    sideFont,
                    theme.SideLabelBrush);
                var labelOrigin = ResolveSideLabelOrigin(n, labelText, scene);
                context.DrawText(labelText, labelOrigin);
            }
        }
    }

    /// <summary>Узел условия в control-flow: заливка и обводка ромба (классический знак ветвления).</summary>
    private static void DrawConditionBranch(DrawingContext context, SkiaGraphVisualTheme theme, GraphLayoutNode n, bool highlighted)
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

    private static IBrush ResolveNodeFill(SkiaGraphVisualTheme theme, GraphLayoutNode node)
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

    private static string BuildNodeGlyph(GraphLayoutNode node, bool useLegendColumn, bool showNodeLegendGlyphs)
    {
        if (node.IsAnchor)
            return "A";
        if (string.Equals(node.Kind, "protected_step", StringComparison.OrdinalIgnoreCase))
            return "T";
        if (IsExitNode(node))
            return "";
        if (node.LegendIndex is { } idx && (useLegendColumn || showNodeLegendGlyphs))
            return idx.ToString(CultureInfo.InvariantCulture);
        if (IsConditionNode(node))
            return "?";
        if (IsHandlerNode(node))
            return "!";
        return "•";
    }

    /// <summary>Подпись спутника: иерархия (related-files); CFG вертикаль — справа, CFG горизонт — под узлом (не в поток).</summary>
    private static Point ResolveSideLabelOrigin(
        GraphLayoutNode node,
        FormattedText labelText,
        GraphLayoutScene scene)
    {
        const double gap = 6;

        if (scene.Presentation == GraphLayoutPresentation.CodeControlFlow
            && scene.ControlFlowMainAxis == GraphControlFlowMainAxis.Horizontal
            && !node.IsAnchor)
        {
            return new Point(
                node.Center.X - labelText.Width / 2,
                node.Center.Y + node.Radius + gap);
        }

        var anchorCenter =
            scene.Presentation == GraphLayoutPresentation.WorkspaceRelatedFiles
                ? scene.Nodes.FirstOrDefault(static n => n.IsAnchor)?.Center
                : null;
        var layout = CascadeIDE.Models.CodeNavigationMapRelatedGraphLayoutKind.Normalize(scene.RelatedFilesLayout);
        if (layout == CascadeIDE.Models.CodeNavigationMapRelatedGraphLayoutKind.TopDown && !node.IsAnchor)
        {
            return new Point(
                node.Center.X - labelText.Width / 2,
                node.Center.Y + node.Radius + gap);
        }

        if (layout == CascadeIDE.Models.CodeNavigationMapRelatedGraphLayoutKind.BottomUp && !node.IsAnchor)
        {
            return new Point(
                node.Center.X - labelText.Width / 2,
                node.Center.Y - node.Radius - gap - labelText.Height);
        }

        if (anchorCenter is { } anchor && !node.IsAnchor
            && layout == CascadeIDE.Models.CodeNavigationMapRelatedGraphLayoutKind.Radial)
        {
            var dx = node.Center.X - anchor.X;
            var dy = node.Center.Y - anchor.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > 4)
            {
                var ux = dx / dist;
                var uy = dy / dist;
                var edge = new Point(
                    node.Center.X + ux * (node.Radius + gap),
                    node.Center.Y + uy * (node.Radius + gap));
                return ux >= 0
                    ? new Point(edge.X, edge.Y - labelText.Height / 2)
                    : new Point(edge.X - labelText.Width, edge.Y - labelText.Height / 2);
            }
        }

        return new Point(
            node.Center.X + node.Radius + gap,
            node.Center.Y - labelText.Height / 2);
    }

    private static string? BuildNodeFullLabel(GraphLayoutNode node, bool useLegendColumn, GraphLayoutScene scene)
    {
        if (useLegendColumn)
            return null;
        if (node.IsAnchor || IsConditionNode(node) || IsExitNode(node))
            return null;
        if (scene.Presentation == GraphLayoutPresentation.WorkspaceRelatedFiles
            && !CascadeIDE.Models.CodeNavigationMapRelatedGraphLayoutKind.IsHierarchy(scene.RelatedFilesLayout))
        {
            var satellites = scene.Nodes.Count(n => !n.IsAnchor);
            if (satellites > 8)
                return null;
        }

        if (scene.ShowNodeLegendGlyphs && node.LegendIndex is > 0)
            return null;

        var label = node.Label?.Trim();
        if (string.IsNullOrWhiteSpace(label))
            return null;
        return label;
    }

    private static bool IsExitNode(GraphLayoutNode node) =>
        string.Equals(node.Kind, ExitStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionNode(GraphLayoutNode node) =>
        string.Equals(node.Kind, ConditionStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsHandlerNode(GraphLayoutNode node) =>
        string.Equals(node.Kind, "handler_step", StringComparison.OrdinalIgnoreCase);
}

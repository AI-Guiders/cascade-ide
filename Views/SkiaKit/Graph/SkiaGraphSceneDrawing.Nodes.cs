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
            else if (n.Shape == GraphNodeShape.Rectangle)
                DrawRectangleNode(context, theme, n, highlighted);
            else
            {
                context.DrawEllipse(ResolveNodeFill(theme, n), theme.NodeStrokePen, n.Center, n.Radius, n.Radius);
                if (highlighted)
                    context.DrawEllipse(null, theme.HighlightedNodePen, n.Center, n.Radius + 3, n.Radius + 3);
            }

            if (n.Shape != GraphNodeShape.Rectangle)
            {
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

    private static void DrawRectangleNode(DrawingContext context, SkiaGraphVisualTheme theme, GraphLayoutNode n, bool highlighted)
    {
        const double padX = 10;
        const double padY = 6;
        var baseSize = ResolveRectangleNodeSize(n.Radius);
        var fontSize = Math.Clamp(baseSize.Height * 0.52, 9, 14);
        // Prefer "card grows to fit text" (instead of forcing ellipsis early). Keep a sane upper bound to avoid absurd cards.
        var maxCardW = 560.0;
        var maxCardH = 150.0;
        var maxTextW = Math.Max(36, maxCardW - padX * 2);

        var raw = (n.Label ?? "").Trim();
        if (raw.Length == 0)
            raw = Path.GetFileName(n.FullPath);

        // Try single-line first, then fallback to 2-line split.
        var single = new FormattedText(
            raw,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.SideLabelTypeface,
            fontSize,
            theme.SideLabelBrush);

        string l1;
        string? l2;
        if (single.Width <= maxTextW)
        {
            l1 = raw;
            l2 = null;
        }
        else
        {
            (l1, l2) = SplitLabelIntoTwoLines(raw);
        }

        var t1 = new FormattedText(
            l1,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            theme.SideLabelTypeface,
            fontSize,
            theme.SideLabelBrush);
        FormattedText? t2 = null;
        if (!string.IsNullOrEmpty(l2))
        {
            t2 = new FormattedText(
                l2,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                theme.SideLabelTypeface,
                fontSize,
                theme.SideLabelBrush);
        }

        // If still too wide, ellipsize the widest line using a conservative char budget.
        if (t1.Width > maxTextW || (t2 is not null && t2.Width > maxTextW))
        {
            var approxCharW = Math.Max(5.5, t1.Width / Math.Max(1, l1.Length));
            var budget = Math.Max(10, (int)Math.Floor(maxTextW / approxCharW));
            l1 = Ellipsize(l1, budget);
            if (!string.IsNullOrEmpty(l2))
                l2 = Ellipsize(l2, budget);
            t1 = new FormattedText(
                l1,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                theme.SideLabelTypeface,
                fontSize,
                theme.SideLabelBrush);
            t2 = string.IsNullOrEmpty(l2)
                ? null
                : new FormattedText(
                    l2,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    theme.SideLabelTypeface,
                    fontSize,
                    theme.SideLabelBrush);
        }

        // Ensure the card really contains the label (with padding), otherwise it looks like “text without a box”.
        var textW = Math.Max(t1.Width, t2?.Width ?? 0);
        var textH = t1.Height + (t2 is null ? 0 : t2.Height + 2);
        var w = Math.Clamp(Math.Max(baseSize.Width, textW + padX * 2), 42, maxCardW);
        var h = Math.Clamp(Math.Max(baseSize.Height, textH + padY * 2), 18, maxCardH);
        var rect = new Rect(n.Center.X - w / 2, n.Center.Y - h / 2, w, h);
        const double corner = 6;
        context.DrawRectangle(ResolveNodeFill(theme, n), theme.NodeStrokePen, rect, corner, corner);
        if (highlighted)
        {
            var hRect = rect.Inflate(3);
            context.DrawRectangle(null, theme.HighlightedNodePen, hRect, corner, corner);
        }

        // Label inside card (up to 2 lines).
        if (t2 is null)
        {
            context.DrawText(t1, new Point(n.Center.X - t1.Width / 2, n.Center.Y - t1.Height / 2));
            return;
        }

        var totalH = t1.Height + 2 + t2.Height;
        var top = n.Center.Y - totalH / 2;
        context.DrawText(t1, new Point(n.Center.X - t1.Width / 2, top));
        context.DrawText(t2, new Point(n.Center.X - t2.Width / 2, top + t1.Height + 2));
    }

    private static (double Width, double Height) ResolveRectangleNodeSize(double radius)
    {
        // Keep size derived only from radius so hit-testing can match without text measurement.
        var w = Math.Clamp(radius * 4.6, 56, 200);
        var h = Math.Clamp(radius * 2.6, 24, 60);
        return (w, h);
    }

    private static (string Line1, string? Line2) SplitLabelIntoTwoLines(string raw)
    {
        // Tokenize by separators and PascalCase boundaries.
        var tokens = SplitLabelTokens(raw);
        if (tokens.Count == 0)
            return (Ellipsize(raw, 24), null);

        // Prefer: 2 lines with roughly balanced length.
        var target = Math.Max(8, tokens.Sum(t => t.Length) / 2);
        var line1 = new List<string>();
        var len1 = 0;
        foreach (var t in tokens)
        {
            var add = (line1.Count == 0 ? 0 : 1) + t.Length;
            if (line1.Count > 0 && len1 + add > target)
                break;
            line1.Add(t);
            len1 += add;
        }

        if (line1.Count == 0)
            line1.Add(tokens[0]);

        var rest = tokens.Skip(line1.Count).ToList();
        var l1 = string.Join(' ', line1);
        if (rest.Count == 0)
            return (l1, null);

        var l2 = string.Join(' ', rest);
        return (l1, l2);
    }

    private static List<string> SplitLabelTokens(string raw)
    {
        var cleaned = raw.Replace('_', ' ').Replace('.', ' ').Replace('-', ' ').Trim();
        if (cleaned.Length == 0)
            return [];

        var list = new List<string>();
        foreach (var part in cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Split PascalCase: ArchitectureForbiddenApiSyntax -> Architecture Forbidden Api Syntax
            var start = 0;
            for (var i = 1; i < part.Length; i++)
            {
                if (char.IsUpper(part[i]) && char.IsLower(part[i - 1]))
                {
                    if (i - start >= 2)
                        list.Add(part[start..i]);
                    start = i;
                }
            }

            if (start < part.Length)
                list.Add(part[start..]);
        }

        return list;
    }

    private static string Ellipsize(string s, int maxChars)
    {
        var t = (s ?? "").Trim();
        if (t.Length <= maxChars)
            return t;
        if (maxChars <= 1)
            return "…";
        return t[..(maxChars - 1)] + "…";
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

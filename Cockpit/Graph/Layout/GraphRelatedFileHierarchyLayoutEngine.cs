#nullable enable
using Avalonia;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Иерархия related-files: якорь — корень, спутники — один уровень (не дерево solution explorer).</summary>
public sealed class GraphRelatedFileHierarchyLayoutEngine : IGraphLayoutEngine
{
    private readonly bool _anchorAtTop;

    public GraphRelatedFileHierarchyLayoutEngine(bool anchorAtTop) => _anchorAtTop = anchorAtTop;

    public GraphLayoutScene Layout(
        GraphDocument doc,
        double width,
        double height,
        CodeNavigationMapDetailLevel detailLevel = CodeNavigationMapDetailLevel.Normal,
        GraphControlFlowMainAxis? controlFlowMainAxisOverride = null)
    {
        if (width <= 0 || height <= 0)
            return EmptyScene(width);

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        var satellites = doc.Nodes
            .Where(n => anchor is null || !string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var margin = Math.Min(
            GraphFileLayoutMetrics.SideLabelMargin,
            Math.Max(28, Math.Min(width, height) * 0.18));
        var innerW = Math.Max(80, width - 2 * margin);
        var innerH = Math.Max(80, height - 2 * margin);
        var cx = width / 2;
        var scale = Math.Clamp(Math.Min(innerW, innerH) / 220.0, 0.58, 1.22);
        var anchorR = 16 * scale;
        var satR = 12 * scale;
        var rowStep = Math.Clamp(anchorR * 2.6 + satR * 2.2, 38, innerH / Math.Max(2, satellites.Count + 1));

        var layouts = new List<GraphLayoutNode>();
        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        var idToRadius = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        Point? anchorCenter = null;

        var nRows = Math.Max(1, satellites.Count + 1);
        var totalSpan = rowStep * (nRows - 1);
        var startY = _anchorAtTop
            ? margin + anchorR
            : margin + innerH - totalSpan - anchorR;
        var anchorY = startY;
        var childStartY = _anchorAtTop ? startY + rowStep : startY - rowStep;

        if (anchor is not null)
        {
            var ac = new Point(cx, anchorY);
            anchorCenter = ac;
            idToCenter[anchor.Id] = ac;
            idToRadius[anchor.Id] = anchorR;
            layouts.Add(MakeNode(anchor, ac, anchorR, isAnchor: true));
        }

        for (var i = 0; i < satellites.Count; i++)
        {
            var sat = satellites[i];
            var y = _anchorAtTop
                ? childStartY + i * rowStep
                : childStartY - i * rowStep;
            var p = new Point(cx, y);
            idToCenter[sat.Id] = p;
            idToRadius[sat.Id] = satR;
            layouts.Add(MakeNode(sat, p, satR, isAnchor: false));
        }

        var edgeLayouts = BuildEdges(doc, anchor, anchorCenter, layouts, idToCenter, idToRadius, satR);
        var layoutKind = _anchorAtTop
            ? CodeNavigationMapRelatedGraphLayoutKind.TopDown
            : CodeNavigationMapRelatedGraphLayoutKind.BottomUp;

        return new GraphLayoutScene
        {
            Nodes = layouts,
            Edges = edgeLayouts,
            Legend = [],
            UseLegendColumn = false,
            LegendColumnLeft = width,
            RelatedFilesLayout = layoutKind
        };
    }

    private static GraphLayoutNode MakeNode(GraphNode n, Point center, double radius, bool isAnchor) =>
        new()
        {
            Id = n.Id,
            Kind = n.Kind,
            FullPath = n.Path,
            Label = TruncateLabel(n.Label),
            Center = center,
            Radius = radius,
            IsAnchor = isAnchor,
            Shape = GraphNodeShape.Circle,
            LineStart = n.LineStart,
            LineEnd = n.LineEnd
        };

    private static List<GraphLayoutEdge> BuildEdges(
        GraphDocument doc,
        GraphNode? anchor,
        Point? anchorCenter,
        List<GraphLayoutNode> layouts,
        Dictionary<string, Point> idToCenter,
        Dictionary<string, double> idToRadius,
        double defaultSatR)
    {
        var edgeLayouts = new List<GraphLayoutEdge>();
        foreach (var e in doc.Edges)
        {
            if (!idToCenter.TryGetValue(e.FromId, out var a))
                continue;
            if (!idToCenter.TryGetValue(e.ToId, out var b))
                continue;
            edgeLayouts.Add(new GraphLayoutEdge
            {
                FromNodeId = e.FromId,
                ToNodeId = e.ToId,
                From = a,
                To = b,
                ToRadius = idToRadius.TryGetValue(e.ToId, out var toR) ? toR : defaultSatR,
                Kind = e.Kind,
                RelationKind = e.RelationKind
            });
        }

        if (edgeLayouts.Count == 0 && anchorCenter is { } ac0)
        {
            foreach (var s in layouts.Where(x => !x.IsAnchor))
                edgeLayouts.Add(new GraphLayoutEdge
                {
                    FromNodeId = anchor?.Id ?? "n0",
                    ToNodeId = s.Id,
                    From = ac0,
                    To = s.Center,
                    ToRadius = s.Radius,
                    Kind = null,
                    RelationKind = null
                });
        }

        return edgeLayouts;
    }

    private static GraphLayoutScene EmptyScene(double width) =>
        new()
        {
            Nodes = [],
            Edges = [],
            Legend = [],
            LegendColumnLeft = width
        };

    private static string TruncateLabel(string label)
    {
        if (label.Length <= GraphControlFlowLayoutMetrics.LabelMaxLength)
            return label;
        return label[..GraphControlFlowLayoutMetrics.LabelTruncateLength] + "…";
    }
}

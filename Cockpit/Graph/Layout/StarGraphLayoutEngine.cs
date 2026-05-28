#nullable enable
using Avalonia;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Звезда: якорь в центре, спутники по окружности (<c>radial</c>).</summary>
public sealed class StarGraphLayoutEngine : IGraphLayoutEngine
{
    public GraphLayoutScene Layout(
        GraphDocument doc,
        double width,
        double height,
        CodeNavigationMapDetailLevel detailLevel = CodeNavigationMapDetailLevel.Normal,
        GraphControlFlowMainAxis? controlFlowMainAxisOverride = null,
        GraphLayoutEngineOptions layoutOptions = default)
    {
        if (width <= 0 || height <= 0)
            return EmptyScene(width);

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        var satellites = doc.Nodes
            .Where(n => anchor is null || !string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var margin = Math.Min(
            GraphFileLayoutMetrics.SideLabelMargin,
            Math.Max(28, Math.Min(width, height) * 0.22));
        var innerW = Math.Max(80, width - 2 * margin);
        var innerH = Math.Max(80, height - 2 * margin);
        var cx = width / 2;
        var cy = height / 2;
        var minDim = Math.Min(innerW, innerH);
        var scale = Math.Clamp(minDim / 220.0, 0.58, ResolveRelatedAutoScaleCeiling(innerW, innerH, satellites.Count));
        var anchorR = 16 * scale;
        var satR = 13 * scale;
        const int singleRingMaxSatellites = 8;

        var layouts = new List<GraphLayoutNode>();
        Point? anchorCenter = null;
        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        var idToRadius = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (anchor is not null)
        {
            var ac = new Point(cx, cy);
            anchorCenter = ac;
            idToCenter[anchor.Id] = ac;
            idToRadius[anchor.Id] = anchorR;
            layouts.Add(MakeNode(anchor, ac, anchorR, isAnchor: true));
        }

        var nSat = satellites.Count;
        var useTwoRings = nSat > singleRingMaxSatellites;
        var innerCount = useTwoRings ? (nSat + 1) / 2 : nSat;
        var (orbitInner, orbitOuter) = GraphFileLayoutMetrics.ResolveRadialOrbits(nSat, innerW, innerH, satR);

        for (var i = 0; i < nSat; i++)
        {
            var sat = satellites[i];
            double orbit;
            double angle;
            if (!useTwoRings)
            {
                orbit = orbitInner;
                angle = nSat == 0 ? 0 : (2 * Math.PI * i / nSat) - Math.PI / 2;
            }
            else if (i < innerCount)
            {
                orbit = orbitInner;
                angle = innerCount == 0 ? 0 : (2 * Math.PI * i / innerCount) - Math.PI / 2;
            }
            else
            {
                var outerN = nSat - innerCount;
                var j = i - innerCount;
                orbit = orbitOuter;
                var stagger = innerCount > 0 ? Math.PI / innerCount : 0;
                angle = outerN == 0 ? 0 : (2 * Math.PI * j / outerN) - Math.PI / 2 + stagger;
            }

            var px = cx + orbit * Math.Cos(angle);
            var py = cy + orbit * Math.Sin(angle);
            var p = new Point(px, py);
            idToCenter[sat.Id] = p;
            idToRadius[sat.Id] = satR;
            layouts.Add(MakeNode(sat, p, satR, isAnchor: false));
        }

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
                ToRadius = idToRadius.TryGetValue(e.ToId, out var toR) ? toR : satR,
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

        return new GraphLayoutScene
        {
            Nodes = layouts,
            Edges = edgeLayouts,
            Legend = [],
            UseLegendColumn = false,
            LegendColumnLeft = width,
            RelatedFilesLayout = CodeNavigationMapRelatedGraphLayoutKind.Radial
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
            Shape = GraphNodeShape.Rectangle,
            LineStart = n.LineStart,
            LineEnd = n.LineEnd
        };

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
        // For related-files we want the renderer to handle wrapping inside cards.
        // Control-flow uses strict budgets; related-files doesn't.
        var s = label?.Trim() ?? "";
        if (s.Length <= 80)
            return s;
        return s[..77] + "…";
    }

    private static double ResolveRelatedAutoScaleCeiling(double innerW, double innerH, int satelliteCount)
    {
        // For related-files, bigger cards are more readable when there's a lot of empty space.
        // Keep a conservative ceiling for dense graphs to avoid overlap.
        var minDim = Math.Min(innerW, innerH);
        if (satelliteCount <= 4 && minDim >= 260)
            return 1.75;
        if (satelliteCount <= 8 && minDim >= 240)
            return 1.55;
        if (satelliteCount <= 12 && minDim >= 220)
            return 1.40;
        return 1.22;
    }
}

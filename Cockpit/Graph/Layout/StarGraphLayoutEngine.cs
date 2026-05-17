#nullable enable
using Avalonia;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Звезда: якорь в центре, спутники по окружности.</summary>
public sealed class StarGraphLayoutEngine : IGraphLayoutEngine
{
    public GraphLayoutScene Layout(GraphDocument doc, double width, double height)
    {
        if (width <= 0 || height <= 0)
            return EmptyScene(width);

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        var satellites = doc.Nodes
            .Where(n => anchor is null || !string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cx = width / 2;
        var cy = height / 2;
        var minDim = Math.Min(width, height);
        var scale = Math.Clamp(minDim / 220.0, 0.58, 1.22);
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
            layouts.Add(new GraphLayoutNode
            {
                Id = anchor.Id,
                Kind = anchor.Kind,
                FullPath = anchor.Path,
                Label = TruncateLabel(anchor.Label),
                Center = ac,
                Radius = anchorR,
                IsAnchor = true,
                Shape = GraphNodeShape.Circle
            });
        }

        var nSat = satellites.Count;
        var useTwoRings = nSat > singleRingMaxSatellites;
        var innerCount = useTwoRings ? (nSat + 1) / 2 : nSat;
        var orbitInner = Math.Max(22, minDim * (useTwoRings ? 0.26 : 0.32));
        var orbitOuter = useTwoRings
            ? Math.Max(orbitInner + satR * 2 + 8, Math.Min(minDim * 0.40, orbitInner + minDim * 0.18))
            : orbitInner;

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
            layouts.Add(new GraphLayoutNode
            {
                Id = sat.Id,
                Kind = sat.Kind,
                FullPath = sat.Path,
                Label = TruncateLabel(sat.Label),
                Center = p,
                Radius = satR,
                IsAnchor = false,
                Shape = GraphNodeShape.Circle
            });
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
            LegendColumnLeft = width
        };
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

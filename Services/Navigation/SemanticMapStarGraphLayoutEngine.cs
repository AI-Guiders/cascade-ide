#nullable enable
using Avalonia;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Звезда: якорь в центре, спутники по окружности (тот же подграф, что отдаёт MCP в режиме <c>subgraph</c>).</summary>
public sealed class SemanticMapStarGraphLayoutEngine : ISemanticMapSubgraphLayoutEngine
{
    public SemanticMapGraphSceneVm Layout(SemanticMapSubgraphDocument doc, double width, double height)
    {
        if (width <= 0 || height <= 0)
            return new SemanticMapGraphSceneVm
            {
                Nodes = [],
                Edges = [],
                Legend = [],
                LegendColumnLeft = width
            };

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        var satellites = doc.Nodes
            .Where(n => anchor is null || !string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cx = width / 2;
        var cy = height / 2;
        const double anchorR = 16;
        const double satR = 13;
        var minDim = Math.Min(width, height);
        // Одно кольцо — чуть компактнее, чтобы граф не «разъезжался» на всю зону; при плотности — два кольца (не одна орбита).
        const int singleRingMaxSatellites = 8;

        var layouts = new List<SemanticMapGraphNodeLayout>();
        Point? anchorCenter = null;
        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        var idToRadius = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (anchor is not null)
        {
            var ac = new Point(cx, cy);
            anchorCenter = ac;
            idToCenter[anchor.Id] = ac;
            idToRadius[anchor.Id] = anchorR;
            layouts.Add(new SemanticMapGraphNodeLayout
            {
                Id = anchor.Id,
                Kind = anchor.Kind,
                FullPath = anchor.Path,
                Label = TruncateLabel(anchor.Label),
                Center = ac,
                Radius = anchorR,
                IsAnchor = true,
                Shape = SemanticMapNodeShape.Circle
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
            layouts.Add(new SemanticMapGraphNodeLayout
            {
                Id = sat.Id,
                Kind = sat.Kind,
                FullPath = sat.Path,
                Label = TruncateLabel(sat.Label),
                Center = p,
                Radius = satR,
                IsAnchor = false,
                Shape = SemanticMapNodeShape.Circle
            });
        }

        var edgeLayouts = new List<SemanticMapGraphEdgeLayout>();
        foreach (var e in doc.Edges)
        {
            if (!idToCenter.TryGetValue(e.FromId, out var a))
                continue;
            if (!idToCenter.TryGetValue(e.ToId, out var b))
                continue;
            edgeLayouts.Add(new SemanticMapGraphEdgeLayout
            {
                FromNodeId = e.FromId,
                ToNodeId = e.ToId,
                From = a,
                To = b,
                ToRadius = idToRadius.TryGetValue(e.ToId, out var toR) ? toR : satR,
                Kind = e.Kind,
                RelatedKind = e.RelatedKind
            });
        }

        // Если рёбер нет, но есть якорь и спутники — линии от центра.
        if (edgeLayouts.Count == 0 && anchorCenter is { } ac0)
        {
            foreach (var s in layouts.Where(x => !x.IsAnchor))
                edgeLayouts.Add(new SemanticMapGraphEdgeLayout
                {
                    FromNodeId = anchor?.Id ?? "n0",
                    ToNodeId = s.Id,
                    From = ac0,
                    To = s.Center,
                    ToRadius = s.Radius,
                    Kind = null,
                    RelatedKind = null
                });
        }

        return new SemanticMapGraphSceneVm
        {
            Nodes = layouts,
            Edges = edgeLayouts,
            Legend = [],
            UseLegendColumn = false,
            LegendColumnLeft = width
        };
    }

    private static string TruncateLabel(string label)
    {
        if (label.Length <= 22)
            return label;
        return label[..19] + "…";
    }
}

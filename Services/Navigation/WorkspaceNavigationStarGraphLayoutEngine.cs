#nullable enable
using Avalonia;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Звезда: якорь в центре, спутники по окружности (тот же подграф, что отдаёт MCP в режиме <c>subgraph</c>).</summary>
public sealed class WorkspaceNavigationStarGraphLayoutEngine : IWorkspaceNavigationGraphLayoutEngine
{
    public SemanticMapGraphSceneVm Layout(WorkspaceNavigationSubgraphDocument doc, double width, double height)
    {
        if (width <= 0 || height <= 0)
            return new SemanticMapGraphSceneVm { Nodes = [], Edges = [] };

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        var satellites = doc.Nodes
            .Where(n => anchor is null || !string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cx = width / 2;
        var cy = height / 2;
        const double anchorR = 16;
        const double satR = 13;
        var orbit = Math.Min(width, height) * 0.36;
        if (orbit < 24)
            orbit = 24;

        var layouts = new List<SemanticMapGraphNodeLayout>();
        Point? anchorCenter = null;
        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);

        if (anchor is not null)
        {
            var ac = new Point(cx, cy);
            anchorCenter = ac;
            idToCenter[anchor.Id] = ac;
            layouts.Add(new SemanticMapGraphNodeLayout
            {
                Id = anchor.Id,
                FullPath = anchor.Path,
                Label = TruncateLabel(anchor.Label),
                Center = ac,
                Radius = anchorR,
                IsAnchor = true
            });
        }

        var nSat = satellites.Count;
        for (var i = 0; i < nSat; i++)
        {
            var sat = satellites[i];
            var angle = nSat == 0 ? 0 : (2 * Math.PI * i / nSat) - Math.PI / 2;
            var px = cx + orbit * Math.Cos(angle);
            var py = cy + orbit * Math.Sin(angle);
            var p = new Point(px, py);
            idToCenter[sat.Id] = p;
            layouts.Add(new SemanticMapGraphNodeLayout
            {
                Id = sat.Id,
                FullPath = sat.Path,
                Label = TruncateLabel(sat.Label),
                Center = p,
                Radius = satR,
                IsAnchor = false
            });
        }

        var edgeLayouts = new List<SemanticMapGraphEdgeLayout>();
        foreach (var e in doc.Edges)
        {
            if (!idToCenter.TryGetValue(e.FromId, out var a))
                continue;
            if (!idToCenter.TryGetValue(e.ToId, out var b))
                continue;
            edgeLayouts.Add(new SemanticMapGraphEdgeLayout { From = a, To = b });
        }

        // Если рёбер нет, но есть якорь и спутники — линии от центра.
        if (edgeLayouts.Count == 0 && anchorCenter is { } ac0)
        {
            foreach (var s in layouts.Where(x => !x.IsAnchor))
                edgeLayouts.Add(new SemanticMapGraphEdgeLayout { From = ac0, To = s.Center });
        }

        return new SemanticMapGraphSceneVm { Nodes = layouts, Edges = edgeLayouts };
    }

    private static string TruncateLabel(string label)
    {
        if (label.Length <= 22)
            return label;
        return label[..19] + "…";
    }
}

#nullable enable
using Avalonia;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Укладка control-flow в формате "полётного плана": основной поток сверху вниз,
/// а узлы одного шага по глубине — в сторону от центральной оси.
/// </summary>
public sealed class WorkspaceNavigationControlFlowGraphLayoutEngine : IWorkspaceNavigationGraphLayoutEngine
{
    public SemanticMapGraphSceneVm Layout(WorkspaceNavigationSubgraphDocument doc, double width, double height)
    {
        if (width <= 0 || height <= 0)
            return new SemanticMapGraphSceneVm { Nodes = [], Edges = [] };

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase))
                     ?? (doc.Nodes.Count > 0 ? doc.Nodes[0] : null);
        if (anchor is null)
            return new SemanticMapGraphSceneVm { Nodes = [], Edges = [] };

        var nodeById = doc.Nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
        var outgoing = BuildOutgoing(doc.Edges);
        var depthById = BuildDepthMap(anchor.Id, outgoing);

        // Узлы, недостижимые от anchor, ставим в хвост (последние уровни), чтобы не терялись.
        var maxDepth = depthById.Count == 0 ? 0 : depthById.Values.Max();
        foreach (var n in doc.Nodes)
        {
            if (depthById.ContainsKey(n.Id))
                continue;
            maxDepth++;
            depthById[n.Id] = maxDepth;
        }

        var levels = depthById
            .GroupBy(kv => kv.Value)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

        const double topPadding = 14;
        const double bottomPadding = 14;
        const double sidePadding = 14;
        const double anchorR = 14;
        const double nodeR = 12;

        var levelCount = Math.Max(1, levels.Count);
        var yStep = Math.Max(24, (height - topPadding - bottomPadding) / Math.Max(1, levelCount - 1));
        var centerX = width / 2;

        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        var idToRadius = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var nodeLayouts = new List<SemanticMapGraphNodeLayout>(doc.Nodes.Count);

        foreach (var (depth, ids) in levels)
        {
            var y = topPadding + depth * yStep;
            var orderedIds = ids
                .OrderBy(id => string.Equals(id, anchor.Id, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var count = orderedIds.Count;
            var minX = sidePadding + nodeR;
            var maxX = Math.Max(minX, width - sidePadding - nodeR);
            for (var i = 0; i < count; i++)
            {
                var id = orderedIds[i];
                if (!nodeById.TryGetValue(id, out var n))
                    continue;

                var x = count == 1
                    ? centerX
                    : minX + (maxX - minX) * i / (count - 1);
                var radius = string.Equals(id, anchor.Id, StringComparison.OrdinalIgnoreCase) ? anchorR : nodeR;
                var point = new Point(x, y);
                idToCenter[id] = point;
                idToRadius[id] = radius;
                nodeLayouts.Add(new SemanticMapGraphNodeLayout
                {
                    Id = n.Id,
                    Kind = n.Kind,
                    FullPath = n.Path,
                    Label = TruncateLabel(n.Label),
                    Center = point,
                    Radius = radius,
                    IsAnchor = string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        var edgeLayouts = new List<SemanticMapGraphEdgeLayout>(doc.Edges.Count);
        foreach (var e in doc.Edges)
        {
            if (!idToCenter.TryGetValue(e.FromId, out var from))
                continue;
            if (!idToCenter.TryGetValue(e.ToId, out var to))
                continue;

            edgeLayouts.Add(new SemanticMapGraphEdgeLayout
            {
                FromNodeId = e.FromId,
                ToNodeId = e.ToId,
                From = from,
                To = to,
                ToRadius = idToRadius.TryGetValue(e.ToId, out var toR) ? toR : nodeR,
                Kind = e.Kind,
                RelatedKind = e.RelatedKind
            });
        }

        return new SemanticMapGraphSceneVm { Nodes = nodeLayouts, Edges = edgeLayouts };
    }

    private static Dictionary<string, List<string>> BuildOutgoing(IReadOnlyList<WorkspaceNavigationSubgraphEdge> edges)
    {
        var outgoing = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in edges)
        {
            if (!outgoing.TryGetValue(e.FromId, out var list))
            {
                list = [];
                outgoing[e.FromId] = list;
            }

            list.Add(e.ToId);
        }

        return outgoing;
    }

    private static Dictionary<string, int> BuildDepthMap(string anchorId, Dictionary<string, List<string>> outgoing)
    {
        var depthById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [anchorId] = 0
        };
        var queue = new Queue<string>();
        queue.Enqueue(anchorId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var depth = depthById[id];
            if (!outgoing.TryGetValue(id, out var next))
                continue;

            foreach (var to in next)
            {
                if (depthById.ContainsKey(to))
                    continue;
                depthById[to] = depth + 1;
                queue.Enqueue(to);
            }
        }

        return depthById;
    }

    private static string TruncateLabel(string label)
    {
        if (label.Length <= 22)
            return label;
        return label[..19] + "…";
    }
}

#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Models;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Сжатие control-flow subgraph на карте (ADR 0053): glance и перегруженный multibranch в normal.</summary>
internal static class CodeNavigationMapControlFlowDeclutter
{
    /// <summary>Glance: убрать все multibranch-рёбра и недостижимое от якоря.</summary>
    private const int NormalFanOutCollapseThreshold = 4;

    /// <summary>Normal: при веере ≥ порога оставить столько значимых исходов + агрегат «+N».</summary>
    private const int NormalFanOutKeepCount = 3;

    public static GraphDocument? TryTransform(in CodeNavigationMapPipelineState state)
    {
        if (!string.Equals(state.MapLevel, CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal))
            return null;

        return state.DetailLevel switch
        {
            CodeNavigationMapDetailLevel.Glance => TryStripAllMultiBranch(state.Subgraph),
            CodeNavigationMapDetailLevel.Normal => TryCompressMultiBranchFanOut(state.Subgraph),
            _ => null
        };
    }

    private static GraphDocument? TryStripAllMultiBranch(GraphDocument doc)
    {
        var edgesWithoutMulti = doc.Edges.Where(e => !IsMultibranchEdge(e)).ToList();
        if (edgesWithoutMulti.Count == doc.Edges.Count)
            return null;

        var anchorId = FindAnchorNodeId(doc);
        var reachable = ReachableForward(anchorId, edgesWithoutMulti);
        var nodes = doc.Nodes.Where(n => reachable.Contains(n.Id)).ToList();
        var kept = new HashSet<string>(reachable, StringComparer.OrdinalIgnoreCase);
        var finalEdges = edgesWithoutMulti.Where(e => kept.Contains(e.FromId) && kept.Contains(e.ToId)).ToList();

        return new GraphDocument
        {
            AnchorPath = doc.AnchorPath,
            Kind = doc.Kind,
            Nodes = nodes,
            Edges = finalEdges
        };
    }

    private static GraphDocument? TryCompressMultiBranchFanOut(GraphDocument doc)
    {
        var nodeById = doc.Nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
        var multiByFrom = doc.Edges
            .Where(IsMultibranchEdge)
            .GroupBy(e => e.FromId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= NormalFanOutCollapseThreshold)
            .ToList();
        if (multiByFrom.Count == 0)
            return null;

        var removedEdgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extraNodes = new List<GraphNode>();
        var extraEdges = new List<GraphEdge>();
        var nextAgg = 0;

        foreach (var group in multiByFrom)
        {
            var ordered = group
                .OrderBy(e => TargetPriority(nodeById.GetValueOrDefault(e.ToId)))
                .ThenBy(e => e.ToId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var keep = ordered.Take(NormalFanOutKeepCount).ToList();
            var drop = ordered.Skip(NormalFanOutKeepCount).ToList();
            if (drop.Count == 0)
                continue;

            foreach (var e in drop)
                removedEdgeKeys.Add(EdgeKey(e));

            var hub = nodeById.GetValueOrDefault(group.Key);
            var aggId = $"mbr_agg_{group.Key}_{nextAgg++}";
            extraNodes.Add(new GraphNode
            {
                Id = aggId,
                Path = hub?.Path ?? doc.AnchorPath,
                Kind = "condition_step",
                Label = $"+{drop.Count}",
                LineStart = hub?.LineStart
            });
            extraEdges.Add(new GraphEdge
            {
                FromId = group.Key,
                ToId = aggId,
                Kind = "MultiBranch",
                RelationKind = "collapsed_branches"
            });
        }

        if (removedEdgeKeys.Count == 0)
            return null;

        var edges = doc.Edges.Where(e => !removedEdgeKeys.Contains(EdgeKey(e))).Concat(extraEdges).ToList();
        var nodeIds = new HashSet<string>(edges.SelectMany(e => new[] { e.FromId, e.ToId }), StringComparer.OrdinalIgnoreCase);
        var nodes = doc.Nodes.Where(n => nodeIds.Contains(n.Id)).Concat(extraNodes).ToList();

        return new GraphDocument
        {
            AnchorPath = doc.AnchorPath,
            Kind = doc.Kind,
            Nodes = nodes,
            Edges = edges
        };
    }

    private static int TargetPriority(GraphNode? node)
    {
        if (node is null)
            return 4;
        if (string.Equals(node.Kind, "call_step", StringComparison.OrdinalIgnoreCase)
            || string.Equals(node.Kind, "loop_step", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(node.Kind, "exit_step", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(node.Kind, "condition_step", StringComparison.OrdinalIgnoreCase))
            return 2;
        return 3;
    }

    private static string EdgeKey(GraphEdge e) => $"{e.FromId}->{e.ToId}";

    private static bool IsMultibranchEdge(GraphEdge e) =>
        !string.IsNullOrWhiteSpace(e.Kind)
        && e.Kind.Contains("multibranch", StringComparison.OrdinalIgnoreCase);

    private static string FindAnchorNodeId(GraphDocument doc)
    {
        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase));
        if (anchor is not null)
            return anchor.Id;
        var n0 = doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        if (n0 is not null)
            return n0.Id;
        return doc.Nodes[0].Id;
    }

    private static HashSet<string> ReachableForward(string anchorId, IReadOnlyList<GraphEdge> edges)
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

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { anchorId };
        var queue = new Queue<string>();
        queue.Enqueue(anchorId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!outgoing.TryGetValue(id, out var next))
                continue;
            foreach (var t in next)
            {
                if (seen.Add(t))
                    queue.Enqueue(t);
            }
        }

        return seen;
    }
}

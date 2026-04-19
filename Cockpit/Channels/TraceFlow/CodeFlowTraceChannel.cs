#nullable enable
namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Code-flow trace channel: computes a lightweight dominant path for current control-flow subgraph.
/// </summary>
public sealed class CodeFlowTraceChannel : ITraceFlowChannel
{
    public TraceFlowChannelSnapshot Build(in TraceFlowChannelContext context)
    {
        var subgraph = context.Subgraph;
        if (subgraph.Nodes.Count == 0)
            return Empty();

        var highlightedNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var highlightedEdgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outgoing = subgraph.Edges
            .GroupBy(e => e.FromId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var start = subgraph.Nodes.FirstOrDefault(n => IsAnchor(n.Kind))?.Id ?? subgraph.Nodes[0].Id;
        var current = start;
        highlightedNodeIds.Add(current);

        var maxSteps = Math.Max(1, subgraph.Edges.Count + 2);
        for (var i = 0; i < maxSteps; i++)
        {
            if (!outgoing.TryGetValue(current, out var nextEdges) || nextEdges.Count == 0)
                break;

            var next = PickDominantEdge(nextEdges);
            highlightedEdgeKeys.Add(MakeEdgeKey(next.FromId, next.ToId));
            highlightedNodeIds.Add(next.ToId);
            current = next.ToId;

            if (IsExit(next.Kind))
                break;
        }

        return new TraceFlowChannelSnapshot(highlightedNodeIds, highlightedEdgeKeys);
    }

    private static TraceFlowChannelSnapshot Empty() =>
        new(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static bool IsAnchor(string? kind) =>
        string.Equals(kind, "anchor", StringComparison.OrdinalIgnoreCase);

    private static bool IsExit(string? kind) =>
        string.Equals(kind, "Exit", StringComparison.OrdinalIgnoreCase);

    private static SemanticMapSubgraphEdge PickDominantEdge(IReadOnlyList<SemanticMapSubgraphEdge> edges)
    {
        if (edges.Count == 1)
            return edges[0];

        var nonExit = edges.FirstOrDefault(e => !IsExit(e.Kind));
        return nonExit ?? edges[0];
    }

    private static string MakeEdgeKey(string fromId, string toId) => $"{fromId}->{toId}";
}

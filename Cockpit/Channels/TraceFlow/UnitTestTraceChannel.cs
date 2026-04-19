#nullable enable

namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Unit-test trace channel (ADR 0036 p.1): contributes test-evidence emphasis over control-flow graph.
/// MVP behavior: when failing/impacted tests are present, highlight all exit nodes/edges.
/// </summary>
public sealed class UnitTestTraceChannel : ITraceFlowChannel
{
    public TraceFlowChannelSnapshot Build(in TraceFlowChannelContext context)
    {
        if (context.ImpactedTestsBadge <= 0)
            return Empty();

        var highlightedNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var highlightedEdgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in context.Subgraph.Nodes)
        {
            if (string.Equals(node.Kind, "exit_step", StringComparison.OrdinalIgnoreCase))
                highlightedNodeIds.Add(node.Id);
        }

        foreach (var edge in context.Subgraph.Edges)
        {
            if (!string.Equals(edge.Kind, "Exit", StringComparison.OrdinalIgnoreCase))
                continue;
            highlightedEdgeKeys.Add($"{edge.FromId}->{edge.ToId}");
            highlightedNodeIds.Add(edge.ToId);
        }

        return new TraceFlowChannelSnapshot(highlightedNodeIds, highlightedEdgeKeys);
    }

    private static TraceFlowChannelSnapshot Empty() =>
        new(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}

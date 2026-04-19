#nullable enable
using CascadeIDE.Cockpit.Channels;

namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Aggregates multiple trace-flow channels into one combined semantic payload.
/// </summary>
public sealed class TraceFlowChannelCoordinator : IChannelCoordinator<TraceFlowChannelContext, TraceFlowChannelSnapshot>
{
    private readonly IReadOnlyList<ITraceFlowChannel> _channels;

    public TraceFlowChannelCoordinator(IEnumerable<ITraceFlowChannel> channels)
    {
        _channels = channels.ToArray();
    }

    public TraceFlowChannelSnapshot Build(in TraceFlowChannelContext context)
    {
        var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var edges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var channel in _channels)
        {
            var snapshot = channel.Build(context);
            nodes.UnionWith(snapshot.HighlightedNodeIds);
            edges.UnionWith(snapshot.HighlightedEdgeKeys);
        }

        return new TraceFlowChannelSnapshot(nodes, edges);
    }
}

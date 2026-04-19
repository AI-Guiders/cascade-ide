#nullable enable

namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Channel payload for flow tracing (ADR 0036 p.1): semantic highlights by node/edge identity.
/// Carries no layout coordinates and no control-specific rendering details.
/// </summary>
public sealed record TraceFlowChannelSnapshot(
    IReadOnlySet<string> HighlightedNodeIds,
    IReadOnlySet<string> HighlightedEdgeKeys);

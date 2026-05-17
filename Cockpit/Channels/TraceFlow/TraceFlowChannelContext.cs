#nullable enable
using CascadeIDE.Cockpit.Graph;

namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Shared channel context for trace-flow family.
/// </summary>
public readonly record struct TraceFlowChannelContext(
    GraphDocument Subgraph,
    int ImpactedTestsBadge,
    string LastTestSummary);

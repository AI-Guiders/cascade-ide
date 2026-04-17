#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Shared channel context for trace-flow family.
/// </summary>
public readonly record struct TraceFlowChannelContext(
    WorkspaceNavigationSubgraphDocument Subgraph,
    int ImpactedTestsBadge,
    string LastTestSummary);

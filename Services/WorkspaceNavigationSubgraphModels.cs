#nullable enable

namespace CascadeIDE.Services;

/// <summary>Разобранный ответ режима <c>subgraph</c> из <see cref="WorkspaceNavigationContextBuilder"/> (тот же JSON, что MCP).</summary>
public sealed class WorkspaceNavigationSubgraphDocument
{
    public required string AnchorPath { get; init; }
    public required IReadOnlyList<WorkspaceNavigationSubgraphNode> Nodes { get; init; }
    public required IReadOnlyList<WorkspaceNavigationSubgraphEdge> Edges { get; init; }
}

public sealed class WorkspaceNavigationSubgraphNode
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required string Kind { get; init; }
    public required string Label { get; init; }
    public string? RelativePath { get; init; }
    public string? Rationale { get; init; }
}

public sealed class WorkspaceNavigationSubgraphEdge
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Kind { get; init; }
    public string? RelatedKind { get; init; }
}

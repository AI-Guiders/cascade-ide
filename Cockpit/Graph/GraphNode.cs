#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>Узел в <see cref="GraphDocument"/> (graph-backed surface, ADR 0067).</summary>
public class GraphNode
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required string Kind { get; init; }
    public required string Label { get; init; }
    public string? RelativePath { get; init; }
    public string? Rationale { get; init; }
    /// <summary>Номер в легенде control flow (1-based).</summary>
    public int? LegendIndex { get; init; }
    public string? LegendText { get; init; }
}

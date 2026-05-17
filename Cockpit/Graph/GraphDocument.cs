#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>
/// Доменный документ graph-backed surface: узлы/рёбра, якорь, <see cref="GraphKind"/> (ADR 0067, 0115).
/// Wire JSON MCP и адаптеры источников сводятся к этой модели до композиции Skia.
/// </summary>
public class GraphDocument
{
    public required string AnchorPath { get; init; }

    /// <summary>Тип графа (<c>graph_kind</c>); при <see cref="GraphKind.Unspecified"/> презентация может выводиться эвристикой инструмента.</summary>
    public GraphKind Kind { get; init; } = GraphKind.Unspecified;

    public required IReadOnlyList<GraphNode> Nodes { get; init; }
    public required IReadOnlyList<GraphEdge> Edges { get; init; }
}

#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>Ребро в <see cref="GraphDocument"/> (graph-backed surface, ADR 0067).</summary>
public class GraphEdge
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    /// <summary>Доменный kind рёбра (control flow, merge, contains, …).</summary>
    public string? Kind { get; init; }
    /// <summary>Смысл связи (<c>relation_kind</c>, ADR 0114); в wire часто <c>related_kind</c>.</summary>
    public string? RelationKind { get; init; }
    /// <summary>Происхождение связи (<c>edge_provenance</c>, ADR 0113) — опционально в wire.</summary>
    public string? EdgeProvenance { get; init; }
}

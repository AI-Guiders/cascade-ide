#nullable enable

namespace CascadeIDE.Services;

/// <summary>
/// Wire-модель подграфа для композиции сцены (тот же JSON, что MCP <c>get_code_navigation_context</c>, режим <c>subgraph</c>).
/// Продуктовый термин <b>Semantic Map</b> — это <b>семантическая карта намерений кода</b>, не общий «граф смысловых связей»; тот же контейнер может нести и другие графы (связанные файлы, дерево модулей) — см. ADR 0065, ось <c>graph_kind</c> (в контракте пока косвенно: уровень карты, виды узлов).
/// <list type="bullet">
/// <item><description><b>Карта кода</b> (control flow, шаги метода, предикаты) — домен <b>CodeNavigation</b> (<see cref="CodeNavigationContextBuilder"/>, <see cref="CodeNavigation.CodeNavigationControlFlowSubgraphBuilder"/>).</description></item>
/// <item><description><b>Зависимости / связанные файлы</b> по эвристикам дерева решения — <b>WorkspaceNavigation</b> (см. те же поля в JSON).</description></item>
/// <item><description><b>Git submodules</b> — отдельно: <b>дерево</b> / GitMap (ADR 0062), не путать с «картой кода» и не с картой файловых зависимостей.</description></item>
/// </list>
/// Префикс <c>SemanticMap*</c> указывает на эту <b>карту и пайплайн композиции сцены</b>, а не на домен; сценарий задаёт <see cref="Kind"/> и источник данных.
/// </summary>
public sealed class SemanticMapSubgraphDocument
{
    public required string AnchorPath { get; init; }
    public required IReadOnlyList<SemanticMapSubgraphNode> Nodes { get; init; }
    public required IReadOnlyList<SemanticMapSubgraphEdge> Edges { get; init; }
}

public sealed class SemanticMapSubgraphNode
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required string Kind { get; init; }
    public required string Label { get; init; }
    public string? RelativePath { get; init; }
    public string? Rationale { get; init; }
    /// <summary>Номер в легенде control flow (1-based); только для узлов подграфа с подписью в JSON.</summary>
    public int? LegendIndex { get; init; }
    /// <summary>Строка для колонки легенды (предикат, вызов, return).</summary>
    public string? LegendText { get; init; }
}

public sealed class SemanticMapSubgraphEdge
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Kind { get; init; }
    public string? RelatedKind { get; init; }
}

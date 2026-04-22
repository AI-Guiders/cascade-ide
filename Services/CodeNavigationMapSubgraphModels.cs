#nullable enable

namespace CascadeIDE.Services;

/// <summary>Канонические строки <c>graph_kind</c> в JSON subgraph (ADR 0065 §6).</summary>
public static class CodeNavigationMapGraphKindWire
{
    /// <summary>Каноническое <c>graph_kind</c> для карты намерений кода (control flow); домен CodeNavigation.</summary>
    public const string CodeIntent = "code_intent_code_navigation_map";

    /// <summary>Устаревшее имя; <see cref="CodeNavigationMapSubgraphJson"/> принимает для совместимости.</summary>
    public const string CodeIntentLegacy = "code_intent_semantic_map";

    public const string RelatedFiles = "related_files";
    public const string RepositoryModuleTree = "repository_module_tree";
}

/// <summary>Тип графа в wire-модели; дублирует <see cref="CodeNavigationMapGraphKindWire"/>.</summary>
public enum CodeNavigationMapGraphKind : byte
{
    /// <summary>Не указано в JSON — клиент выводит по уровню карты (см. <see cref="Navigation.CodeNavigationMapPresentationResolver"/>).</summary>
    Unspecified = 0,
    CodeIntent = 1,
    RelatedFiles = 2,
    RepositoryModuleTree = 3
}

/// <summary>
/// Wire-модель подграфа для композиции сцены (тот же JSON, что MCP <c>get_code_navigation_context</c>, режим <c>subgraph</c>).
/// Продуктово в UI — <b>карта кода</b> (семантическая карта <b>намерений кода</b>), не общий «граф смысловых связей»; тот же контейнер может нести и другие графы (связанные файлы, дерево модулей) — см. ADR 0065, ось <c>graph_kind</c>.
/// <list type="bullet">
/// <item><description><b>Карта кода</b> (control flow, шаги метода, предикаты) — домен <b>CodeNavigation</b> (<see cref="CodeNavigationContextBuilder"/>, <see cref="CodeNavigation.CodeNavigationControlFlowSubgraphBuilder"/>).</description></item>
/// <item><description><b>Зависимости / связанные файлы</b> по эвристикам дерева решения — <b>WorkspaceNavigation</b> (см. те же поля в JSON).</description></item>
/// <item><description><b>Git submodules</b> — отдельно: <b>дерево</b> / GitMap (ADR 0062), не путать с «картой кода» и не с картой файловых зависимостей.</description></item>
/// </list>
/// Префикс типов <c>CodeNavigationMap*</c> (в т.ч. сцена/рендер) указывает на <b>карту и композицию</b>; сценарий данных задаёт <see cref="Kind"/> и источник.
/// </summary>
public sealed class CodeNavigationMapSubgraphDocument
{
    public required string AnchorPath { get; init; }

    /// <summary>Тип графа в payload (<c>graph_kind</c>); при <see cref="CodeNavigationMapGraphKind.Unspecified"/> презентация выводится по уровню карты.</summary>
    public CodeNavigationMapGraphKind GraphKind { get; init; } = CodeNavigationMapGraphKind.Unspecified;

    public required IReadOnlyList<CodeNavigationMapSubgraphNode> Nodes { get; init; }
    public required IReadOnlyList<CodeNavigationMapSubgraphEdge> Edges { get; init; }
}

public sealed class CodeNavigationMapSubgraphNode
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

public sealed class CodeNavigationMapSubgraphEdge
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Kind { get; init; }
    public string? RelatedKind { get; init; }
}

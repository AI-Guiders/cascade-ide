#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>Канонические строки <c>graph_kind</c> в JSON subgraph (ADR 0065 §6).</summary>
public static class GraphKindWire
{
    /// <summary>Карта намерений кода (control flow); домен CodeNavigation.</summary>
    public const string CodeIntent = "code_intent_code_navigation_map";

    /// <summary>Устаревшее имя; <see cref="GraphDocumentJson"/> принимает для совместимости.</summary>
    public const string CodeIntentLegacy = "code_intent_semantic_map";

    public const string RelatedFiles = "related_files";
    public const string RepositoryModuleTree = "repository_module_tree";
}

/// <summary>Тип графа в доменной модели graph-backed surface (ADR 0067, 0065).</summary>
public enum GraphKind : byte
{
    /// <summary>Не указано в JSON — клиент выводит по уровню карты / контексту инструмента.</summary>
    Unspecified = 0,
    CodeIntent = 1,
    RelatedFiles = 2,
    RepositoryModuleTree = 3
}

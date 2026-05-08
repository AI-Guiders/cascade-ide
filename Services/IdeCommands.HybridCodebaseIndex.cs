namespace CascadeIDE.Services;

/// <summary>Команды Hybrid Codebase Index (паритет имён с MCP <c>hybrid-codebase-index</c>, ADR 0105/0106).</summary>
public static partial class IdeCommands
{
    // ——— Hybrid Codebase Index (паритет tool name с внешним MCP)

    /// <summary>Статус локального индекса (как tool <c>codebase_index_status</c>). workspace_path и solution_path опциональны — по умолчанию текущее открытое решение. args: workspace_path?:string, solution_path?:string; returns: json; example: {"workspace_path":"D:\\repo"}.</summary>
    public const string CodebaseIndexStatus = "codebase_index_status";

    /// <summary>Гибридный поиск по индексу (как tool <c>codebase_index_search</c>). query обязателен; workspace/solution по умолчанию — текущее решение. args: workspace_path?:string, solution_path?:string, query:string, top_n?:integer, semantic?:boolean, alpha?:number, beta?:number, vec_top_k?:integer, path_prefix?:string, exclude_path_prefixes?:string[], extensions?:string[]; returns: json; example: {"query":"HybridIndexOrchestrator"}.</summary>
    public const string CodebaseIndexSearch = "codebase_index_search";

    /// <summary>Explain одного hit (как tool <c>codebase_index_explain</c>). hit_id обязателен. args: workspace_path?:string, solution_path?:string, hit_id:integer; returns: json; example: {"hit_id":1}.</summary>
    public const string CodebaseIndexExplain = "codebase_index_explain";

    /// <summary>Переиндексация: по умолчанию инкремент (как tool <c>codebase_index_reindex</c>); full_rebuild=true — полная перестройка. args: workspace_path?:string, solution_path?:string, full_rebuild?:boolean; returns: json; example: {}.</summary>
    public const string CodebaseIndexReindex = "codebase_index_reindex";
}

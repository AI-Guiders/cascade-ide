namespace CascadeIDE.Services;

/// <summary>Заметки агента и hot-context (partial IdeCommands).</summary>
public static partial class IdeCommands
{
    /// <summary>Записать заметки агента в каталог решения. args: content:string; returns: text; example: {"content":"notes"}.</summary>
    public const string WriteAgentNotes = "write_agent_notes";
    /// <summary>Прочитать заметки агента из каталога решения. returns: text.</summary>
    public const string ReadAgentNotes = "read_agent_notes";
    /// <summary>Добавить блок в конец заметок агента. args: content:string; returns: text; example: {"content":"\\n# Update\\n..."}.</summary>
    public const string AppendAgentNotes = "append_agent_notes";
    /// <summary>Список ревизий заметок агента. args: limit?:integer; returns: json; example: {"limit":20}.</summary>
    public const string ListAgentNotesRevisions = "list_agent_notes_revisions";
    /// <summary>Откатить заметки к ревизии (или к последней). args: revision_file?:string; returns: text; example: {"revision_file":"20260402-120000-write-acde123.md"}.</summary>
    public const string RollbackAgentNotes = "rollback_agent_notes";
    /// <summary>Прочитать только hot-context (L0/L1) без архивного хвоста. args: active_scope?:string; returns: json; example: {"active_scope":"door-to-singularity"}.</summary>
    public const string ReadHotContext = "read_hot_context";
    /// <summary>Router-first контекст пакет по запросу. args: query:string, active_scope?:string, max_sections?:integer, max_chars?:integer; returns: json; example: {"query":"CascadeIDE notes structure","max_sections":5,"max_chars":12000}.</summary>
    public const string RouteContext = "route_context";
    /// <summary>Health-check памяти: размер hot-context и рекомендации. args: active_scope?:string; returns: json; example: {"active_scope":"door-to-singularity"}.</summary>
    public const string MemoryHealth = "memory_health";
    /// <summary>Ужать hot-context (preview/apply). args: apply?:boolean; returns: json; example: {"apply":false}.</summary>
    public const string CompactHotContext = "compact_hot_context";
    /// <summary>Поиск по архивной ревизии заметок с контекстом строк. args: query:string, revision_file?:string, head_limit?:integer, context_lines?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":10,"context_lines":2}.</summary>
    public const string ExtractFromArchive = "extract_from_archive";
    /// <summary>Вставить/обновить секцию заметок агента по section_id (маркерный блок). args: section_id:string, content:string; returns: text; example: {"section_id":"active","content":"ActiveProjectId: cascade-ide"}.</summary>
    public const string UpsertAgentNotesSection = "upsert_agent_notes_section";
    /// <summary>Поиск по заметкам агента (case-insensitive) с возвратом совпадающих строк. args: query:string, head_limit?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":20}.</summary>
    public const string SearchAgentNotes = "search_agent_notes";
}

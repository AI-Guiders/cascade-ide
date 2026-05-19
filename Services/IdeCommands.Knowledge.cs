namespace CascadeIDE.Services;

/// <summary>Knowledge API (partial IdeCommands).</summary>
public static partial class IdeCommands
{
    /// <summary>Прочитать knowledge-файл. Корень: knowledge_path, knowledge_root_id (group, …) или primary из TOML. args: file_path:string, knowledge_path?:string, knowledge_root_id?:string, offset?:integer, limit?:integer; returns: text; example: {"file_path":"META/integrity-core.md","offset":2,"limit":20}.</summary>
    public const string ReadKnowledgeFile = "read_knowledge_file";
    /// <summary>Список knowledge-файлов. args: subdir?:string, knowledge_path?:string, knowledge_root_id?:string; returns: json; example: {"subdir":"work","knowledge_root_id":"group"}.</summary>
    public const string ListKnowledgeFiles = "list_knowledge_files";
    /// <summary>Записать knowledge-файл (полная замена). Запись только в primary; read-only roots отклоняются. args: file_path:string, content:string, knowledge_path?:string, knowledge_root_id?:string, save_revision?:boolean; returns: text; example: {"file_path":"META/x.md","content":"# Hi","save_revision":true}.</summary>
    public const string WriteKnowledgeFile = "write_knowledge_file";
    /// <summary>Добавить блок в конец knowledge-файла. args: file_path:string, content:string, knowledge_path?:string, knowledge_root_id?:string, save_revision?:boolean; returns: text; example: {"file_path":"META/x.md","content":"more","save_revision":true}.</summary>
    public const string AppendKnowledgeFile = "append_knowledge_file";
    /// <summary>Вставить/обновить секцию в knowledge-файле по section_id. args: file_path:string, section_id:string, content:string, knowledge_path?:string, knowledge_root_id?:string, save_revision?:boolean; returns: text; example: {"file_path":"index.md","section_id":"foo","content":"body"}.</summary>
    public const string UpsertKnowledgeSection = "upsert_knowledge_section";
    /// <summary>Удалить knowledge-файл. args: file_path:string, knowledge_path?:string, knowledge_root_id?:string; returns: text; example: {"file_path":"tmp.md"}.</summary>
    public const string DeleteKnowledgeFile = "delete_knowledge_file";
    /// <summary>Удалить секцию из knowledge-файла. args: file_path:string, section_id:string, knowledge_path?:string, knowledge_root_id?:string; returns: text; example: {"file_path":"index.md","section_id":"foo"}.</summary>
    public const string DeleteKnowledgeSection = "delete_knowledge_section";
}

namespace CascadeIDE.Services;

/// <summary>Список MCP-тулов IDE и команды редактора/документов (ide_execute_command).</summary>
public static partial class IdeCommands
{
    // ——— MCP / редактор
    /// <summary>Список MCP-тулов, которые IDE публикует (name/description/inputSchema). returns: json.</summary>
    public const string ListTools = "list_tools";

    /// <summary>Открыть файл в редакторе IDE. args: path:string; returns: text; example: {"path":"C:\\tmp\\a.txt"}.</summary>
    public const string OpenFile = "open_file";
    /// <summary>Загрузить решение (.sln/.slnx/.slnf) и обновить дерево решения. args: path:string; returns: text; example: {"path":"D:\\Experiments\\PersonalCursorFolder\\Financial\\software\\open\\cascade-ide\\CascadeIDE.slnx"}.</summary>
    public const string LoadSolution = "load_solution";
    /// <summary>Выделить диапазон в редакторе (1-based). args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":10}.</summary>
    public const string Select = "select";
    /// <summary>Поставить брейкпоинт. args: file_path:string, line:integer, condition?:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}.</summary>
    public const string SetBreakpoint = "set_breakpoint";
    /// <summary>Снять брейкпоинт. args: file_path:string, line:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}.</summary>
    public const string RemoveBreakpoint = "remove_breakpoint";
    /// <summary>Показать Markdown-превью в отдельном окне. args: title:string, content:string; returns: text; example: {"title":"Plan","content":"- step 1\n- step 2"}.</summary>
    public const string ShowPreview = "show_preview";
    /// <summary>Показать превью текущего файла из редактора в отдельном окне (контент берётся из IDE). returns: text.</summary>
    public const string ShowEditorPreview = "show_editor_preview";
    /// <summary>Запросить подтверждение у пользователя. args: message:string; returns: text; example: {"message":"Продолжить?"}. Возвращает <c>ok</c>/<c>cancel</c>.</summary>
    public const string RequestConfirmation = "request_confirmation";
    /// <summary>Состояние активного редактора: файл, каретка, выделение. args: max_preview_chars?:integer; returns: json; example: {"max_preview_chars":0}.</summary>
    public const string GetEditorState = "get_editor_state";
    /// <summary>Текст активного редактора по диапазону строк (1-based). args: start_line:integer, end_line:integer; returns: json; example: {"start_line":1,"end_line":40}.</summary>
    public const string GetEditorContentRange = "get_editor_content_range";
    /// <summary>Полный текст открытого документа по пути (или текущего). Модель вкладки, не снимок темы. returns: text.</summary>
    public const string GetOpenDocumentText = "get_open_document_text";
    /// <summary>Применить текстовую правку в открытом документе. args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer, new_text:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":1,"new_text":"// hi\n"}.</summary>
    public const string ApplyEdit = "apply_edit";
    /// <summary>Перейти на позицию (и опционально выделить диапазон). args: file_path:string, line:integer, column:integer, end_line?:integer, end_column?:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":10,"column":1}.</summary>
    public const string GoToPosition = "go_to_position";
}

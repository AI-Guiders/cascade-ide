namespace CascadeIDE.Services;

/// <summary>Тема, layout UI, контролы, панель отладки (partial IdeCommands).</summary>
public static partial class IdeCommands
{
    /// <summary>Снимок темы UI и лэйаута (включая resolved-ресурсы). returns: json.</summary>
    public const string GetUiTheme = "get_ui_theme";
    /// <summary>Применить тему UI из JSON. args: theme:string; returns: text; example: {"theme":"{}"}.</summary>
    public const string SetUiTheme = "set_ui_theme";
    /// <summary>Дерево UI по всем окнам верхнего уровня: JSON с массивом windows (role, window_type, title, is_active, root — то же дерево, что раньше для MainWindow). returns: json.</summary>
    public const string GetUiLayout = "get_ui_layout";
    /// <summary>Цвета под курсором (прямые и effective). returns: json.</summary>
    public const string GetColorsUnderCursor = "get_colors_under_cursor";
    /// <summary>Снимок внешнего вида контрола (под курсором или по имени). args: name?:string; returns: json; example: {"name":"BuildButton"}.</summary>
    public const string GetControlAppearance = "get_control_appearance";
    /// <summary>Изменить раскладку/позицию контрола. args: name:string, layout:string; returns: text; example: {"name":"BuildButton","layout":"{}"}.</summary>
    public const string SetControlLayout = "set_control_layout";
    /// <summary>Установить текст в контроле ввода. args: name:string, text:string; returns: text; example: {"name":"ChatInput","text":"hi"}.</summary>
    public const string SetControlText = "set_control_text";
    /// <summary>Клик по кнопке (под курсором или по имени). args: name?:string; returns: text; example: {"name":"BuildButton"}.</summary>
    public const string ClickControl = "click_control";
    /// <summary>Отправить хоткей в контрол. args: keys:string, name?:string; returns: text; example: {"keys":"Ctrl+S"}.</summary>
    public const string SendKeys = "send_keys";
    /// <summary>Передать фокус контролу (под курсором или по имени). args: name?:string; returns: text; example: {"name":"Editor"}.</summary>
    public const string SetFocus = "set_focus";
    /// <summary>Подсветить контрол рамкой в том окне, где он находится (главное, вспомогательное и т.д.). args: name?:string; returns: text; example: {"name":"BuildButton"}.</summary>
    public const string HighlightControl = "highlight_control";
    /// <summary>Изменить размер панели. args: panel:string, width?:integer, height?:integer; returns: text; example: {"panel":"terminal","height":300}.</summary>
    public const string SetPanelSize = "set_panel_size";
    /// <summary>Список поддерживаемых языков подсветки редактора. returns: json.</summary>
    public const string GetSupportedEditorLanguages = "get_supported_editor_languages";

    /// <summary>Показать брейкпоинты отладчика в IDE. args: breakpoints:object[]; returns: text; example: {"breakpoints":[]}.</summary>
    public const string ShowBreakpoints = "show_breakpoints";
    /// <summary>Показать текущую позицию отладки (файл/строка). args: file_path?:string, line?:integer; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs","line":1}.</summary>
    public const string ShowDebugPosition = "show_debug_position";
    /// <summary>Показать стек/переменные отладки в панели Debug. args: stack_frames?:object[], variables?:object[]; returns: text; example: {"stack_frames":[],"variables":[]}.</summary>
    public const string ShowDebugState = "show_debug_state";
    /// <summary>Добавить контрол в UI (Debug). args: parent_name:string, control_type:string, content?:string, name?:string; returns: text; example: {"parent_name":"Root","control_type":"TextBlock","content":"Hi"}.</summary>
    public const string AddControl = "add_control";
}

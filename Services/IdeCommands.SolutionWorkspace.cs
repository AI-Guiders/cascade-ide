namespace CascadeIDE.Services;

/// <summary>Решение, обозреватель, диагностики и сводка workspace (ide_execute_command).</summary>
public static partial class IdeCommands
{
    /// <summary>Короткая информация о текущем решении/файле/выделении в дереве. returns: json.</summary>
    public const string GetSolutionInfo = "get_solution_info";
    /// <summary>Список файлов и дерево решения (Solution Explorer). returns: json.</summary>
    public const string GetSolutionFiles = "get_solution_files";
    /// <summary>Поиск текста по workspace через ripgrep: вызывается команда <c>rg</c> из PATH (Windows/Linux/macOS — поставь пакетом или с релиза). Явный путь: только <c>rg_path</c>. args: pattern:string, subpath?:string, fixed_string?:boolean, glob?:string, max_matches?:integer, rg_path?:string; returns: json; example: {\"pattern\":\"LoadSolution\",\"glob\":\"*.cs\",\"max_matches\":50}.</summary>
    public const string SearchWorkspaceText = "search_workspace_text";
    /// <summary>Диагностики текущего открытого .cs (ошибки/предупреждения). returns: json.</summary>
    public const string GetCurrentFileDiagnostics = "get_current_file_diagnostics";
    /// <summary>Единая сводка состояния IDE (solution/editor/build/diagnostics...). returns: json.</summary>
    public const string GetIdeState = "get_ide_state";
    /// <summary>Только CDS (<c>CockpitSurfaceState</c>): тот же payload, что поле <c>cockpit_surface</c> в <c>get_ide_state</c>. returns: json. Для <c>--agent-contract</c> без полной сводки.</summary>
    public const string GetCockpitSurface = "get_cockpit_surface";
    /// <summary>Диагностика загрузки UI-режимов: пути к UiModes, TOML vs встроенный fallback, список id в меню (почему может не быть Flight). returns: json.</summary>
    public const string GetUiModesDiagnostics = "get_ui_modes_diagnostics";
    /// <summary>Контекст навигации по коду (ADR 0039, CNC): связанные файлы или мини-подграф. Виды связей — partial_peer project_peer xaml_codebehind_pair test_counterpart same_namespace same_directory. Имена preset — из settings.toml <c>[code_navigation]</c> / <c>[[code_navigation.presets]]</c>. args: mode:string, file_path?:string, line?:integer, column?:integer, max_related?:integer, max_nodes?:integer, max_edges?:integer, preset?:string, include_kinds?:string[], exclude_kinds?:string[], level?:string; returns: json; example: {"mode":"related","file_path":"src/Foo.cs","preset":"no_namespace_noise","level":"controlFlow"}.</summary>
    public const string GetCodeNavigationContext = "get_code_navigation_context";
    /// <summary>Выбрать сообщение в чате по индексу (0-based), в т.ч. для Skia-поверхности. args: index:integer; returns: text; example: {"index":0}.</summary>
    public const string ChatSelectMessage = "chat_select_message";
    /// <summary>Сместить выбор на предыдущее сообщение чата (keyboard-first). returns: text.</summary>
    public const string ChatSelectPrevMessage = "chat_select_prev_message";
    /// <summary>Сместить выбор на следующее сообщение чата (keyboard-first). returns: text.</summary>
    public const string ChatSelectNextMessage = "chat_select_next_message";
    /// <summary>Переключить у выбранного thinking-сообщения свёрнутый/полный вид. returns: text.</summary>
    public const string ChatToggleSelectedThinking = "chat_toggle_selected_thinking";
    /// <summary>Переключить настройку show_thinking_in_history (keyboard-first toggle). returns: text.</summary>
    public const string ChatToggleShowThinkingInHistory = "chat_toggle_show_thinking_in_history";
    /// <summary>Выбрать предыдущую тему в overview (циклически). returns: text.</summary>
    public const string ChatSelectPrevThread = "chat_select_prev_thread";
    /// <summary>Выбрать следующую тему в overview (циклически). returns: text.</summary>
    public const string ChatSelectNextThread = "chat_select_next_thread";
    /// <summary>Открыть detail выбранной темы. returns: text.</summary>
    public const string ChatOpenSelectedThread = "chat_open_selected_thread";
    /// <summary>Вернуться в overview тем (карточки). returns: text.</summary>
    public const string ChatShowThreadOverview = "chat_show_thread_overview";
    /// <summary>Включить/выключить сжатый spine в исходящих сообщениях агенту. returns: text.</summary>
    public const string ChatToggleProductSpineInAgentContext = "chat_toggle_product_spine_in_agent_context";
    /// <summary>Прочитать сквозную линию продукта (spine) сессии. returns: json.</summary>
    public const string ChatGetProductSpine = "chat_get_product_spine";
    /// <summary>Обновить spine (частично): переданные поля перезаписываются; milestones — многострочный текст (веха на строку). args: line_title?:string, current_focus?:string, milestones?:string, include_in_agent_context?:boolean; returns: text; example: {"current_focus":"Topic cards + spine MCP","milestones":"ADR 0096\\nMCP get/set"}.</summary>
    public const string ChatSetProductSpine = "chat_set_product_spine";
    /// <summary>Получить выбранное сообщение чата (индекс, роль, контент) в JSON. returns: json.</summary>
    public const string ChatGetSelectedMessage = "chat_get_selected_message";
    /// <summary>Заменить текст ответа ассистента по стабильному message_id; в лог пишется message_edited. args: message_id:string, new_content:string, reason?:string; returns: json; example: {"message_id":"a1b2c3d4e5f6789012345678901234ab","new_content":"fixed text"}.</summary>
    public const string ChatEditMessage = "chat_edit_message";
    /// <summary>Экспорт текущего чата в читаемый Markdown (роли, индексы, message_id). Поддерживаемый сценарий — явно подвести итоги длинной сессии: экспорт, затем краткое смысловое резюме и согласование с пользователем (см. MCP-PROTOCOL.md, раздел «Подведение итогов сессии чата»). args: write_file?:boolean, file_name?:string; returns: json; example: {"write_file":true}.</summary>
    public const string ChatExportReadable = "chat_export_readable";
}

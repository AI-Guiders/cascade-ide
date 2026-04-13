namespace CascadeIDE.Services;

/// <summary>Решение, обозреватель, диагностики и сводка workspace (ide_execute_command).</summary>
public static partial class IdeCommands
{
    /// <summary>Короткая информация о текущем решении/файле/выделении в дереве. returns: json.</summary>
    public const string GetSolutionInfo = "get_solution_info";
    /// <summary>Список файлов и дерево решения (Solution Explorer). returns: json.</summary>
    public const string GetSolutionFiles = "get_solution_files";
    /// <summary>Диагностики текущего открытого .cs (ошибки/предупреждения). returns: json.</summary>
    public const string GetCurrentFileDiagnostics = "get_current_file_diagnostics";
    /// <summary>Единая сводка состояния IDE (solution/editor/build/diagnostics...). returns: json.</summary>
    public const string GetWorkspaceState = "get_workspace_state";
    /// <summary>Диагностика загрузки UI-режимов: пути к UiModes, TOML vs встроенный fallback, список id в меню (почему может не быть Flight). returns: json.</summary>
    public const string GetUiModesDiagnostics = "get_ui_modes_diagnostics";
    /// <summary>Контекст навигации (ADR 0039): связанные файлы или мини-подграф. args: mode:string, file_path?:string, line?:integer, column?:integer, max_related?:integer, max_nodes?:integer, max_edges?:integer; returns: json; example: {"mode":"related","file_path":"src/Foo.cs","max_related":24}.</summary>
    public const string GetWorkspaceNavigationContext = "get_workspace_navigation_context";
}

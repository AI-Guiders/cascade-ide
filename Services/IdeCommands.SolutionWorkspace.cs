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
}

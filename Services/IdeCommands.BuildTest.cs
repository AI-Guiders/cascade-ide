namespace CascadeIDE.Services;

/// <summary>Сборка, тесты, форматирование, метрики и вывод сборки (ide_execute_command).</summary>
public static partial class IdeCommands
{
    /// <summary>Сборка решения (структурированный результат). returns: json.</summary>
    public const string Build = "build";
    /// <summary>Сборка решения (структурированный результат). То же, что <c>build</c>; выделено для совместимости/алиасов. returns: json.</summary>
    public const string BuildStructured = "build_structured";
    /// <summary>Запустить тесты решения. returns: json.</summary>
    public const string RunTests = "run_tests";
    /// <summary>Запустить затронутые тесты по changed_paths (или fallback на полный прогон). args: changed_paths?:string[]; returns: json; example: {"changed_paths":["a.cs","b.cs"]}.</summary>
    public const string RunAffectedTests = "run_affected_tests";
    /// <summary>Запустить code cleanup (<c>dotnet format</c>). args: include_path?:string; returns: json; example: {"include_path":"src"}.</summary>
    public const string RunCodeCleanup = "run_code_cleanup";
    /// <summary>Метрики кода (LOC/классы/методы/цикломатика). args: scope?:string, path?:string; returns: json; example: {"scope":"solution","path":"."}.</summary>
    public const string GetCodeMetrics = "get_code_metrics";
    /// <summary>Текст панели «Вывод сборки» + цвета оформления. returns: json.</summary>
    public const string GetBuildOutput = "get_build_output";
}

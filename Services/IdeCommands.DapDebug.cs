namespace CascadeIDE.Services;

/// <summary>Встроенный отладчик DAP / netcoredbg (partial <see cref="IdeCommands"/>).</summary>
public static partial class IdeCommands
{
    // ——— DAP / netcoredbg (паритет с dotnet-debug-mcp)
    /// <summary>Проверка доступности встроенной отладки. returns: text.</summary>
    public const string DebugPing = "debug_ping";
    /// <summary>Запустить отладку (netcoredbg DAP): с путями — workspace_path, target_path (.dll/.exe); без путей (мелодия dl / аккорд) — как F5: стартовый проект или диалог .dll/.exe. Опционально netcoredbg_path, program_args. returns: text; example: {"workspace_path":"D:\\\\proj","target_path":"samples\\\\DebugTarget\\\\bin\\\\Debug\\\\net10.0\\\\DebugTarget.dll"}.</summary>
    public const string DebugLaunch = "debug_launch";
    /// <summary>Подключиться к процессу по PID. args: workspace_path:string, process_id:integer, target_path?:string, netcoredbg_path?:string; returns: text; example: {"workspace_path":"D:\\\\proj","process_id":12345}.</summary>
    public const string DebugAttach = "debug_attach";
    /// <summary>Продолжить выполнение (DAP continue). returns: text.</summary>
    public const string DebugContinue = "debug_continue";
    /// <summary>Шаг через строку (DAP next). returns: text.</summary>
    public const string DebugStepOver = "debug_step_over";
    /// <summary>Шаг с заходом (DAP stepIn). returns: text.</summary>
    public const string DebugStepInto = "debug_step_into";
    /// <summary>Шаг с выходом (DAP stepOut). returns: text.</summary>
    public const string DebugStepOut = "debug_step_out";
    /// <summary>Завершить сессию отладки (dispose DAP). returns: text.</summary>
    public const string DebugStop = "debug_stop";
    /// <summary>Стек вызовов (DAP stackTrace). returns: text.</summary>
    public const string DebugStackTrace = "debug_stack_trace";
    /// <summary>Переменные кадра. args: frame_index?:integer; returns: text; example: {"frame_index":0}.</summary>
    public const string DebugVariables = "debug_variables";
    /// <summary>JSON: канонический снимок встроенной DAP-сессии (ADR 0002). returns: json.</summary>
    public const string GetDebugSnapshot = "get_debug_snapshot";
}

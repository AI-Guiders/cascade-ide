namespace CascadeIDE.Services;

/// <summary>Каноническое представление брейкпоинта в IDE debug snapshot.</summary>
public readonly record struct DebugBreakpointSnapshot(string File, int Line, string? Condition);

/// <summary>Корневые переменные одного DAP-scope (Locals, Closures, …), без рекурсивного раскрытия.</summary>
public readonly record struct DebugVariableRootScope(string ScopeName, IReadOnlyList<DebugVariableRow> Roots);

/// <summary>Одна переменная в снимке; дети подгружаются по <see cref="VariablesReference"/> в UI.</summary>
public readonly record struct DebugVariableRow(
    string Name,
    string Value,
    string? Type,
    int VariablesReference = 0,
    int? NamedVariables = null,
    int? IndexedVariables = null);

/// <summary>
/// Единый in-process снимок отладки (ADR 0002): DAP сессия обновляет его; UI, omni, MCP read tools читают одну модель.
/// </summary>
public readonly record struct DebugSessionSnapshot(
    bool HasActiveSession,
    bool IsExecutionStopped,
    string? StoppedFile,
    int StoppedLine,
    string? ExceptionText,
    IReadOnlyList<DebugBreakpointSnapshot> Breakpoints,
    IReadOnlyList<(string Name, string? File, int Line)> StackFrames,
    IReadOnlyList<DebugVariableRootScope> VariableRootScopes,
    int VariablesFrameIndex)
{
    public static DebugSessionSnapshot Empty { get; } = new(
        HasActiveSession: false,
        IsExecutionStopped: false,
        StoppedFile: null,
        StoppedLine: 0,
        ExceptionText: null,
        Breakpoints: Array.Empty<DebugBreakpointSnapshot>(),
        StackFrames: Array.Empty<(string Name, string? File, int Line)>(),
        VariableRootScopes: Array.Empty<DebugVariableRootScope>(),
        VariablesFrameIndex: 0);
}

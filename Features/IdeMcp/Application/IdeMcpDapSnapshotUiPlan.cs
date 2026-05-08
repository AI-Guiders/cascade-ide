namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Инструкции для UI после <see cref="CascadeIDE.Services.DebugSessionSnapshot"/> (без I/O: проверку файлов делает VM).
/// </summary>
public readonly record struct IdeMcpDapSnapshotUiPlan(
    bool MfdPrimedForCurrentStopNext,
    bool ActivateInstrumentationDockAndDebugStack,
    string? DebugPositionFile,
    int DebugPositionLine,
    bool ShouldAttemptOpenStoppedSource,
    string? StoppedSourcePathForOpenAttempt,
    int DebugStackSelectedIndex);

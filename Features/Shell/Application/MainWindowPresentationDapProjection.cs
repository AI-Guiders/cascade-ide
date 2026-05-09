namespace CascadeIDE.Features.Shell.Application;

/// <summary>Проекции DAP-состояния для геттеров presentation (без логики на VM).</summary>
public static class MainWindowPresentationDapProjection
{
    public static bool IsDebugExecutionPaused(bool hasActiveSession, bool executionStopped) =>
        hasActiveSession && executionStopped;

    public static bool IsDebugExecutionRunning(bool hasActiveSession, bool executionStopped) =>
        hasActiveSession && !executionStopped;
}

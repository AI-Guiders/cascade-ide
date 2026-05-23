namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Состояние прогрева solution (ADR 0141).</summary>
public enum SolutionWarmupLifecycle
{
    Idle,
    Running,
    Ready,
    Partial,
    Cancelled,
}

/// <summary>Сигнал IDE о прогреве при смене solution.</summary>
public sealed record SolutionWarmupStateChanged(
    string WorkspaceRoot,
    string? SolutionPath,
    SolutionWarmupLifecycle Lifecycle,
    string? Detail);

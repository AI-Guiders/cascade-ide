namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Фазы учёта времени agent run (ADR 0148 §8).</summary>
public enum AgentRunPhaseKind
{
    Reasoning,
    Environment,
    Blocked,
    IdleUser,
}

public sealed record AgentRunPhaseChanged(
    string RunId,
    AgentRunPhaseKind Phase);

public sealed record AgentRunStarted(
    string RunId,
    string VerifySnapshotId,
    string VerifyPolicy,
    string? SolutionPath);

public sealed record AgentRunCompleted(
    string RunId,
    bool Green,
    string MaxRungReached,
    IReadOnlyList<AgentTimeSlice> TimeSlices);

public sealed record AgentTimeSlice(
    AgentRunPhaseKind Phase,
    double DurationSeconds,
    string? Detail);

public enum AgentEnvironmentTaskState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    Died,
}

public sealed record AgentEnvironmentTaskChanged(
    string TaskId,
    string RunId,
    string Kind,
    AgentEnvironmentTaskState State,
    string? ProgressMessage);

public sealed record AgentEnvironmentTaskCompleted(
    string TaskId,
    string RunId,
    string Kind,
    string ResultSummary,
    int DurationMs);

public sealed record AgentEnvironmentTaskDied(
    string TaskId,
    string RunId,
    string HostKind,
    int? ExitCode,
    string? StderrTail);

public sealed record AgentVerifyEpochStale(
    string RunId,
    string VerifySnapshotId,
    string Reason);

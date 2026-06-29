namespace CascadeIDE.Features.Agent.Harness;

public sealed record AgentHarnessTelemetry(
    int SessionUserTurnCount,
    bool CheckpointDue,
    int? NextCheckpointAtTurn,
    bool HotContextLoaded,
    string? HotContextScope);

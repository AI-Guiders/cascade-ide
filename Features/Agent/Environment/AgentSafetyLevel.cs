namespace CascadeIDE.Features.Agent.Environment;

public static class AgentSafetyLevel
{
    public const string Observe = "safety.observe";
    public const string Confirm = "safety.confirm";
    public const string Autonomous = "safety.autonomous";

    public static bool IsAutonomous(string? level) =>
        string.Equals(level, Autonomous, StringComparison.OrdinalIgnoreCase);

    public static bool IsObserve(string? level) =>
        string.Equals(level, Observe, StringComparison.OrdinalIgnoreCase);

    public static bool IsConfirm(string? level) =>
        string.Equals(level, Confirm, StringComparison.OrdinalIgnoreCase);
}

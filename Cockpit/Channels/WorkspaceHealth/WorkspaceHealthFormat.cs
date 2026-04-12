namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Чистое форматирование строк Workspace Health (без доступа к VM/DAP).
/// См. <see cref="WorkspaceHealthProvider"/> — живые зависимости.
/// </summary>
public static class WorkspaceHealthFormat
{
    public static WorkspaceHealthSegmentInput BuildSegment(bool isBuilding) =>
        new(
            isBuilding ? "Build: running…" : "Build: idle",
            isBuilding ? "BUILD…" : "READY",
            IsBuildRunning: isBuilding);

    public static WorkspaceHealthSegmentInput TestsSegment(string? lastTestSummary, int impactedTestsBadge)
    {
        var line = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? $"Tests: {lastTestSummary}"
            : $"Tests: impacted {impactedTestsBadge}";
        var cockpit = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? (lastTestSummary.Length > 36 ? string.Concat(lastTestSummary.AsSpan(0, 33), "…") : lastTestSummary)
            : $"imp {impactedTestsBadge}";
        return new WorkspaceHealthSegmentInput(line, cockpit);
    }

    public static WorkspaceHealthSegmentInput DebugSegment(
        bool hasActiveSession,
        bool executionStopped,
        int stackFrameCount,
        int variableCount)
    {
        if (!hasActiveSession)
            return new WorkspaceHealthSegmentInput("Debug: idle", "DBG · —");

        if (executionStopped)
        {
            var line = $"Debug: paused · frames {stackFrameCount}, vars {variableCount}";
            var shortLine = $"DBG · pause · {stackFrameCount}fr";
            return new WorkspaceHealthSegmentInput(line, shortLine);
        }

        return new WorkspaceHealthSegmentInput("Debug: running…", "DBG · run");
    }

    public static WorkspaceHealthSegmentInput GitSegment(string gitLine, string gitCockpitShort) =>
        new(gitLine, gitCockpitShort);

    /// <summary>Собирает снимок из уже вычисленных скаляров (удобно для тестов и провайдера).</summary>
    public static WorkspaceHealthInputSnapshot Compose(
        bool isBuilding,
        string? lastTestSummary,
        int impactedTestsBadge,
        bool hasDebugSession,
        bool debugExecutionStopped,
        int debugStackFrameCount,
        int debugVariableCount,
        string gitLine,
        string gitCockpitShort) =>
        new(
            Build: BuildSegment(isBuilding),
            Tests: TestsSegment(lastTestSummary, impactedTestsBadge),
            Debug: DebugSegment(hasDebugSession, debugExecutionStopped, debugStackFrameCount, debugVariableCount),
            Git: GitSegment(gitLine, gitCockpitShort));
}

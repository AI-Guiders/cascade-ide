namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Чистое форматирование строк Workspace Health (без доступа к VM/DAP).
/// См. <see cref="IdeHealthProvider"/> — живые зависимости.
/// </summary>
public static class IdeHealthFormat
{
    public static IdeHealthSegmentInput BuildSegment(bool isBuilding) =>
        new(
            isBuilding ? "Build: running…" : "Build: idle",
            isBuilding ? "BUILD…" : "READY",
            IsBuildRunning: isBuilding);

    public static IdeHealthSegmentInput TestsSegment(string? lastTestSummary, int impactedTestsBadge)
    {
        var line = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? $"Tests: {lastTestSummary}"
            : $"Tests: impacted {impactedTestsBadge}";
        var cockpit = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? (lastTestSummary.Length > 36 ? string.Concat(lastTestSummary.AsSpan(0, 33), "…") : lastTestSummary)
            : $"imp {impactedTestsBadge}";
        return new IdeHealthSegmentInput(line, cockpit);
    }

    public static IdeHealthSegmentInput DebugSegment(
        bool hasActiveSession,
        bool executionStopped,
        int stackFrameCount,
        int variableCount)
    {
        if (!hasActiveSession)
            return new IdeHealthSegmentInput("Debug: idle", "DBG · —");

        if (executionStopped)
        {
            var line = $"Debug: paused · frames {stackFrameCount}, vars {variableCount}";
            var shortLine = $"DBG · pause · {stackFrameCount}fr";
            return new IdeHealthSegmentInput(line, shortLine);
        }

        return new IdeHealthSegmentInput("Debug: running…", "DBG · run");
    }

    public static IdeHealthSegmentInput GitSegment(string gitLine, string gitCockpitShort) =>
        new(gitLine, gitCockpitShort);

    /// <summary>Собирает снимок из уже вычисленных скаляров (удобно для тестов и провайдера).</summary>
    public static IdeHealthInputSnapshot Compose(
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

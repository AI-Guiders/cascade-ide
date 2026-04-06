namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Чистое форматирование строк телеметрии воркспейса (без доступа к VM/DAP).
/// См. <see cref="WorkspaceTelemetryProvider"/> — живые зависимости.
/// </summary>
public static class WorkspaceTelemetryFormat
{
    public static WorkspaceTelemetrySegmentInput BuildSegment(bool isBuilding) =>
        new(
            isBuilding ? "Build: running…" : "Build: idle",
            isBuilding ? "BUILD…" : "READY",
            IsBuildRunning: isBuilding);

    public static WorkspaceTelemetrySegmentInput TestsSegment(string? lastTestSummary, int impactedTestsBadge)
    {
        var line = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? $"Tests: {lastTestSummary}"
            : $"Tests: impacted {impactedTestsBadge}";
        var cockpit = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? (lastTestSummary.Length > 36 ? string.Concat(lastTestSummary.AsSpan(0, 33), "…") : lastTestSummary)
            : $"imp {impactedTestsBadge}";
        return new WorkspaceTelemetrySegmentInput(line, cockpit);
    }

    public static WorkspaceTelemetrySegmentInput DebugSegment(
        bool hasActiveSession,
        bool executionStopped,
        int stackFrameCount,
        int variableCount)
    {
        if (!hasActiveSession)
            return new WorkspaceTelemetrySegmentInput("Debug: idle", "DBG · —");

        if (executionStopped)
        {
            var line = $"Debug: paused · frames {stackFrameCount}, vars {variableCount}";
            var shortLine = $"DBG · pause · {stackFrameCount}fr";
            return new WorkspaceTelemetrySegmentInput(line, shortLine);
        }

        return new WorkspaceTelemetrySegmentInput("Debug: running…", "DBG · run");
    }

    public static WorkspaceTelemetrySegmentInput GitSegment(string gitLine, string gitCockpitShort) =>
        new(gitLine, gitCockpitShort);

    /// <summary>Собирает снимок из уже вычисленных скаляров (удобно для тестов и провайдера).</summary>
    public static WorkspaceTelemetryInputSnapshot Compose(
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

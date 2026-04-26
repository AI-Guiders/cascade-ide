namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// CCU «текст из скаляров» (реализует <see cref="ICockpitComputeUnit"/>, ADR 0097): чистое форматирование IDE Health без VM/DAP.
/// Собирает <see cref="IdeHealthSegmentInput"/>; живые зависимости — в <see cref="IdeHealthSnapshotUnit"/>.
/// </summary>
public sealed class IdeHealthFormattingUnit : ICockpitComputeUnit
{
    /// <summary>Единственный экземпляр свёртки (без состояния).</summary>
    public static IdeHealthFormattingUnit Default { get; } = new();

    private IdeHealthFormattingUnit()
    {
    }

    public IdeHealthSegmentInput BuildSegment(bool isBuilding) =>
        BuildSegment(new BuildStateSnapshot(isBuilding));

    public IdeHealthSegmentInput BuildSegment(BuildStateSnapshot s)
    {
        if (s.IsBuilding)
        {
            return new IdeHealthSegmentInput(
                "Build: running…",
                "BUILD…",
                IsBuildRunning: true,
                Stratum: IdeHealthStratum.Solution,
                Scope: IdeHealthScope.Solution);
        }

        var line = s.LastBuildSucceeded switch
        {
            true when s.LastExitCode is int ok => $"Build: idle · last OK (exit {ok})",
            true => "Build: idle · last OK",
            false when s.LastExitCode is int fail => $"Build: idle · last failed (exit {fail})",
            false => "Build: idle · last failed",
            _ => "Build: idle"
        };
        return new IdeHealthSegmentInput(
            line,
            "READY",
            IsBuildRunning: false,
            Stratum: IdeHealthStratum.Solution,
            Scope: IdeHealthScope.Solution);
    }

    public IdeHealthSegmentInput TestsSegment(string? lastTestSummary, int impactedTestsBadge)
    {
        var line = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? $"Tests: {lastTestSummary}"
            : $"Tests: impacted {impactedTestsBadge}";
        var cockpit = !string.IsNullOrWhiteSpace(lastTestSummary)
            ? (lastTestSummary.Length > 36 ? string.Concat(lastTestSummary.AsSpan(0, 33), "…") : lastTestSummary)
            : $"imp {impactedTestsBadge}";
        return new IdeHealthSegmentInput(line, cockpit, Stratum: IdeHealthStratum.Solution, Scope: IdeHealthScope.Solution);
    }

    public IdeHealthSegmentInput DebugSegment(
        bool hasActiveSession,
        bool executionStopped,
        int stackFrameCount,
        int variableCount)
    {
        if (!hasActiveSession)
            return new IdeHealthSegmentInput("Debug: idle", "DBG · —", Stratum: IdeHealthStratum.Solution, Scope: IdeHealthScope.Solution);

        if (executionStopped)
        {
            var line = $"Debug: paused · frames {stackFrameCount}, vars {variableCount}";
            var shortLine = $"DBG · pause · {stackFrameCount}fr";
            return new IdeHealthSegmentInput(line, shortLine, Stratum: IdeHealthStratum.Solution, Scope: IdeHealthScope.Solution);
        }

        return new IdeHealthSegmentInput("Debug: running…", "DBG · run", Stratum: IdeHealthStratum.Solution, Scope: IdeHealthScope.Solution);
    }

    public IdeHealthSegmentInput GitSegment(string gitLine, string gitCockpitShort) =>
        new(gitLine, gitCockpitShort, Stratum: IdeHealthStratum.Workspace);

    public IdeHealthSegmentInput ProjectBuildSegment(string projectPath, bool isBuilding) =>
        ProjectBuildSegment(projectPath, new BuildStateSnapshot(isBuilding));

    public IdeHealthSegmentInput ProjectBuildSegment(string projectPath, BuildStateSnapshot s)
    {
        if (s.IsBuilding)
        {
            return new IdeHealthSegmentInput(
                $"Build[{projectPath}]: running…",
                "BUILD…",
                IsBuildRunning: true,
                Stratum: IdeHealthStratum.Solution,
                Scope: IdeHealthScope.Project,
                ProjectPath: projectPath);
        }

        var tail = s.LastBuildSucceeded switch
        {
            true when s.LastExitCode is int ok => $"idle · last OK (exit {ok})",
            true => "idle · last OK",
            false when s.LastExitCode is int fail => $"idle · last failed (exit {fail})",
            false => "idle · last failed",
            _ => "idle"
        };
        return new IdeHealthSegmentInput(
            $"Build[{projectPath}]: {tail}",
            "READY",
            IsBuildRunning: false,
            Stratum: IdeHealthStratum.Solution,
            Scope: IdeHealthScope.Project,
            ProjectPath: projectPath);
    }

    public IdeHealthSegmentInput ProjectTestsSegment(string projectPath, string? summary, int impactedTestsBadge)
    {
        var normalizedSummary = string.IsNullOrWhiteSpace(summary)
            ? $"impacted {impactedTestsBadge}"
            : summary;
        return new IdeHealthSegmentInput(
            $"Tests[{projectPath}]: {normalizedSummary}",
            normalizedSummary.Length > 36 ? string.Concat(normalizedSummary.AsSpan(0, 33), "…") : normalizedSummary,
            Stratum: IdeHealthStratum.Solution,
            Scope: IdeHealthScope.Project,
            ProjectPath: projectPath);
    }

    public IdeHealthSegmentInput ProjectDebugSegment(string projectPath, string summary) =>
        new(
            $"Debug[{projectPath}]: {summary}",
            summary.Length > 36 ? string.Concat(summary.AsSpan(0, 33), "…") : summary,
            Stratum: IdeHealthStratum.Solution,
            Scope: IdeHealthScope.Project,
            ProjectPath: projectPath);

    /// <summary>Собирает снимок из уже вычисленных скаляров (удобно для тестов и провайдера).</summary>
    public IdeHealthInputSnapshot Compose(
        BuildStateSnapshot buildState,
        string? lastTestSummary,
        int impactedTestsBadge,
        bool hasDebugSession,
        bool debugExecutionStopped,
        int debugStackFrameCount,
        int debugVariableCount,
        string gitLine,
        string gitCockpitShort) =>
        IdeHealthStrataComposer.Compose(
            new IdeHealthWorkspaceInput(GitSegment(gitLine, gitCockpitShort)),
            new IdeHealthSolutionInput(
                BuildSegment(buildState),
                TestsSegment(lastTestSummary, impactedTestsBadge),
                DebugSegment(hasDebugSession, debugExecutionStopped, debugStackFrameCount, debugVariableCount)));
}

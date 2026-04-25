namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// <strong>CCU</strong> «сбор снимка канала» (ADR 0097): <see cref="IdeHealthInputSnapshot"/> из скаляров, делегатов и DAP-сессии (ADR 0036 п.1).
/// Не тянет <c>UiChromeViewModel</c> / Instrumentation — подставляет корень (<see cref="ViewModels.MainWindowViewModel"/>). Продуктовое имя канала — IDE Health (ADR 0089).
/// </summary>
public sealed class IdeHealthSnapshotUnit : Channels.WorkspaceHealth.IIdeHealthChannel
{
    private readonly Func<bool> _isBuilding;
    private readonly Func<string> _lastTestSummary;
    private readonly Func<int> _impactedTestsBadge;
    private readonly Func<string?> _startupProjectPath;
    private readonly Services.IdeDapDebugSession _dapDebug;
    private readonly Func<string> _workspaceHealthGitLine;
    private readonly Func<string> _workspaceHealthGitCockpitShort;

    public IdeHealthSnapshotUnit(
        Func<bool> isBuilding,
        Func<string> lastTestSummary,
        Func<int> impactedTestsBadge,
        Func<string?> startupProjectPath,
        Services.IdeDapDebugSession dapDebug,
        Func<string> workspaceHealthGitLine,
        Func<string> workspaceHealthGitCockpitShort)
    {
        _isBuilding = isBuilding;
        _lastTestSummary = lastTestSummary;
        _impactedTestsBadge = impactedTestsBadge;
        _startupProjectPath = startupProjectPath;
        _dapDebug = dapDebug;
        _workspaceHealthGitLine = workspaceHealthGitLine;
        _workspaceHealthGitCockpitShort = workspaceHealthGitCockpitShort;
    }

    public IdeHealthInputSnapshot Build(in Channels.WorkspaceHealth.IdeHealthChannelContext context)
    {
        var isBuilding = _isBuilding();
        var testSummary = _lastTestSummary();
        var impactedTestsBadge = _impactedTestsBadge();
        var dap = _dapDebug.GetSnapshot();
        var startupProjectPath = _startupProjectPath();
        var hasStartupProject = !string.IsNullOrWhiteSpace(startupProjectPath);
        var hasProjectSignal = isBuilding || !string.IsNullOrWhiteSpace(testSummary);
        var hasProjectScope = hasStartupProject && hasProjectSignal;

        var build = hasProjectScope
            ? IdeHealthFormattingUnit.Default.ProjectBuildSegment(startupProjectPath!, isBuilding)
            : IdeHealthFormattingUnit.Default.BuildSegment(isBuilding);

        var tests = hasProjectScope
            ? IdeHealthFormattingUnit.Default.ProjectTestsSegment(startupProjectPath!, testSummary, impactedTestsBadge)
            : IdeHealthFormattingUnit.Default.TestsSegment(testSummary, impactedTestsBadge);

        var debugSummary = !dap.HasActiveSession
            ? "idle"
            : dap.IsExecutionStopped
                ? $"paused · frames {dap.StackFrames.Count}, vars {dap.VariableRootScopes.Sum(s => s.Roots.Count)}"
                : "running…";
        var debug = hasProjectScope
            ? IdeHealthFormattingUnit.Default.ProjectDebugSegment(startupProjectPath!, debugSummary)
            : IdeHealthFormattingUnit.Default.DebugSegment(
                dap.HasActiveSession,
                dap.IsExecutionStopped,
                dap.StackFrames.Count,
                dap.VariableRootScopes.Sum(s => s.Roots.Count));

        return new IdeHealthInputSnapshot(
            Build: build,
            Tests: tests,
            Debug: debug,
            Git: IdeHealthFormattingUnit.Default.GitSegment(_workspaceHealthGitLine(), _workspaceHealthGitCockpitShort()));
    }
}

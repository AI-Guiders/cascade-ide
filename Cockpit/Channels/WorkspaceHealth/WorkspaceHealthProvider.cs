namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Слой <strong>канала</strong> Workspace Health (ADR 0036 п.1): собирает <see cref="WorkspaceHealthInputSnapshot"/> из скаляров и делегатов.
/// Не ссылается на <c>UiChromeViewModel</c>, Instrumentation VM и прочие фичи — их подставляет корень композиции (<see cref="ViewModels.MainWindowViewModel"/>).
/// </summary>
public sealed class WorkspaceHealthProvider : IWorkspaceHealthChannel
{
    private readonly Func<bool> _isBuilding;
    private readonly Func<string> _lastTestSummary;
    private readonly Func<int> _impactedTestsBadge;
    private readonly Services.IdeDapDebugSession _dapDebug;
    private readonly Func<int> _debugStackFrameCount;
    private readonly Func<int> _debugVariableCount;
    private readonly Func<string> _workspaceHealthGitLine;
    private readonly Func<string> _workspaceHealthGitCockpitShort;

    public WorkspaceHealthProvider(
        Func<bool> isBuilding,
        Func<string> lastTestSummary,
        Func<int> impactedTestsBadge,
        Services.IdeDapDebugSession dapDebug,
        Func<int> debugStackFrameCount,
        Func<int> debugVariableCount,
        Func<string> workspaceHealthGitLine,
        Func<string> workspaceHealthGitCockpitShort)
    {
        _isBuilding = isBuilding;
        _lastTestSummary = lastTestSummary;
        _impactedTestsBadge = impactedTestsBadge;
        _dapDebug = dapDebug;
        _debugStackFrameCount = debugStackFrameCount;
        _debugVariableCount = debugVariableCount;
        _workspaceHealthGitLine = workspaceHealthGitLine;
        _workspaceHealthGitCockpitShort = workspaceHealthGitCockpitShort;
    }

    public WorkspaceHealthInputSnapshot Build(in WorkspaceHealthChannelContext context) =>
        WorkspaceHealthFormat.Compose(
            _isBuilding(),
            _lastTestSummary(),
            _impactedTestsBadge(),
            _dapDebug.HasActiveSession,
            _dapDebug.IsExecutionStopped,
            _debugStackFrameCount(),
            _debugVariableCount(),
            _workspaceHealthGitLine(),
            _workspaceHealthGitCockpitShort());
}

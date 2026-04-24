using System.Linq;

namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Слой <strong>канала</strong> Workspace Health (ADR 0036 п.1): собирает <see cref="IdeHealthInputSnapshot"/> из скаляров и делегатов.
/// Не ссылается на <c>UiChromeViewModel</c>, Instrumentation VM и прочие фичи — их подставляет корень композиции (<see cref="ViewModels.MainWindowViewModel"/>).
/// </summary>
public sealed class IdeHealthProvider : IIdeHealthChannel
{
    private readonly Func<bool> _isBuilding;
    private readonly Func<string> _lastTestSummary;
    private readonly Func<int> _impactedTestsBadge;
    private readonly Services.IdeDapDebugSession _dapDebug;
    private readonly Func<string> _workspaceHealthGitLine;
    private readonly Func<string> _workspaceHealthGitCockpitShort;

    public IdeHealthProvider(
        Func<bool> isBuilding,
        Func<string> lastTestSummary,
        Func<int> impactedTestsBadge,
        Services.IdeDapDebugSession dapDebug,
        Func<string> workspaceHealthGitLine,
        Func<string> workspaceHealthGitCockpitShort)
    {
        _isBuilding = isBuilding;
        _lastTestSummary = lastTestSummary;
        _impactedTestsBadge = impactedTestsBadge;
        _dapDebug = dapDebug;
        _workspaceHealthGitLine = workspaceHealthGitLine;
        _workspaceHealthGitCockpitShort = workspaceHealthGitCockpitShort;
    }

    public IdeHealthInputSnapshot Build(in IdeHealthChannelContext context) =>
        IdeHealthFormat.Compose(
            _isBuilding(),
            _lastTestSummary(),
            _impactedTestsBadge(),
            _dapDebug.GetSnapshot().HasActiveSession,
            _dapDebug.GetSnapshot().IsExecutionStopped,
            _dapDebug.GetSnapshot().StackFrames.Count,
            _dapDebug.GetSnapshot().VariableRootScopes.Sum(s => s.Roots.Count),
            _workspaceHealthGitLine(),
            _workspaceHealthGitCockpitShort());
}

namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;
using CascadeIDE.Services;

/// <summary>
/// <strong>CCU</strong> «сбор снимка канала» (ADR 0097): <see cref="IdeHealthInputSnapshot"/> только из <see cref="IDataBus"/> (события домена, ADR 0099).
/// Не тянет <c>UiChromeViewModel</c> / DAP / git напрямую. Продуктовое имя канала — IDE Health (ADR 0089).
/// </summary>
[ComputingUnit]
[DataBusSubscriber("ide-health-ccu")]
public sealed class IdeHealthSnapshotUnit : Channels.WorkspaceHealth.IIdeHealthChannel, IDisposable
{
    private readonly IdeHealthScopeDecisionUnit _scopeDecision = IdeHealthScopeDecisionUnit.Default;
    private readonly IdeHealthBuildTestsUnit _buildTests = IdeHealthBuildTestsUnit.Default;
    private readonly IdeHealthDebugSegmentUnit _debugSegment = IdeHealthDebugSegmentUnit.Default;
    private readonly IdeHealthGitSegmentUnit _gitSegment = IdeHealthGitSegmentUnit.Default;
    private readonly IdeHealthIdeHostUnit _ideHostUnit = IdeHealthIdeHostUnit.Default;
    private readonly IDisposable? _buildStateSubscription;
    private readonly IDisposable? _testsStateSubscription;
    private readonly IDisposable? _debugStateSubscription;
    private readonly IDisposable? _gitStateSubscription;
    private readonly IDisposable? _ideHostStateSubscription;
    private readonly IDisposable? _startupProjectSubscription;
    private readonly object _buildSnapshotLock = new();
    private BuildStateSnapshot _buildSnapshot = BuildStateSnapshot.Empty;
    private volatile bool _hasTestsStateFromBus;
    private string _latestTestsSummaryFromBus = "";
    private int _latestImpactedTestsBadgeFromBus;
    private DebugSessionSnapshot _latestDebugSnapshot = DebugSessionSnapshot.Empty;
    private string _latestGitLine = "";
    private string _latestGitCockpitShort = "";
    private string? _latestStartupProjectPath;
    private IdeHostStateChanged _latestIdeHost;
    private bool _disposed;

    public IdeHealthSnapshotUnit(IDataBus dataBus)
    {
        _buildStateSubscription = dataBus.Subscribe<BuildStateChanged>(evt =>
        {
            lock (_buildSnapshotLock)
                _buildSnapshot = BuildStateSnapshotUnit.Apply(_buildSnapshot, evt);
        });
        _testsStateSubscription = dataBus.Subscribe<TestsStateChanged>(evt =>
        {
            _latestTestsSummaryFromBus = evt.Summary ?? "";
            _latestImpactedTestsBadgeFromBus = evt.ImpactedBadge;
            _hasTestsStateFromBus = true;
        });
        _debugStateSubscription = dataBus.Subscribe<DebugStateChanged>(evt =>
        {
            _latestDebugSnapshot = evt.Snapshot;
        });
        _gitStateSubscription = dataBus.Subscribe<GitStateChanged>(evt =>
        {
            _latestGitLine = evt.Line;
            _latestGitCockpitShort = evt.CockpitShort;
        });
        _ideHostStateSubscription = dataBus.Subscribe<IdeHostStateChanged>(evt => _latestIdeHost = evt);
        _startupProjectSubscription = dataBus.Subscribe<StartupProjectPathChanged>(evt =>
        {
            _latestStartupProjectPath = evt.ProjectPath;
        });
    }

    public IdeHealthInputSnapshot Build(in Channels.WorkspaceHealth.IdeHealthChannelContext context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BuildStateSnapshot buildState;
        lock (_buildSnapshotLock)
            buildState = _buildSnapshot;

        var testSummary = _hasTestsStateFromBus ? _latestTestsSummaryFromBus : "";
        var impactedTestsBadge = _hasTestsStateFromBus ? _latestImpactedTestsBadgeFromBus : 0;
        var dap = _latestDebugSnapshot;
        var scopeDecision = _scopeDecision.Decide(_latestStartupProjectPath, buildState.IsBuilding, testSummary);
        var buildTests = _buildTests.Compose(scopeDecision, buildState, testSummary, impactedTestsBadge);

        var build = buildTests.Build;
        var tests = buildTests.Tests;

        var debug = _debugSegment.Compose(scopeDecision, dap);
        var git = _gitSegment.Compose(_latestGitLine, _latestGitCockpitShort);
        var ideHost = _ideHostUnit.Compose(_latestIdeHost);

        return IdeHealthStrataComposer.Compose(
            new IdeHealthWorkspaceInput(git),
            new IdeHealthSolutionInput(build, tests, debug),
            ideHost);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _buildStateSubscription?.Dispose();
        _testsStateSubscription?.Dispose();
        _debugStateSubscription?.Dispose();
        _gitStateSubscription?.Dispose();
        _ideHostStateSubscription?.Dispose();
        _startupProjectSubscription?.Dispose();
    }

}

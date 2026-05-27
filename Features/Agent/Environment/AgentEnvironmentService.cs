using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;
using CascadeIDE.Services;
using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

public interface IAgentEnvironmentService
{
    AgentEnvironmentStatusSnapshot GetStatus();

    AgentVerifyStartResult StartVerify(
        string solutionPath,
        AgentVerifyPolicy policy,
        AgentSandboxProfile? sandboxProfile = null);

    /// <summary>Batch entry (W6): optional worktree profile для long autonomous runs.</summary>
    AgentVerifyStartResult StartVerifyBatch(AgentVerifyBatchRequest request);

    AgentSandboxPrepareResult PrepareSandbox(AgentSandboxProfile profile, string? workspaceRoot = null);

    bool CancelActive();

    AgentEnvironmentLastRunSummary? GetLastRun();

    AgentVerifyEpochTracker EpochTracker { get; }

    IAgentOrchestrator Orchestrator { get; }

    AgentAeeRoslynBridge RoslynBridge { get; }

    void NotifyCideWindowFocus(bool focused);
}

/// <summary>Verify ladder + runner (ADR 0148 W1–W6).</summary>
public sealed class AgentEnvironmentService : IAgentEnvironmentService
{
    private readonly IDataBus _dataBus;
    private readonly AgentEnvironmentSettings _settings;
    private readonly BuildTestJobCoordinator _coordinator;
    private readonly IGitCommandRunner? _gitRunner;
    private readonly Func<string?>? _getWorkspaceRootForSnapshot;
    private IBuildTestHost _buildTestHost;
    private VerificationLadder _ladder;
    private readonly AgentSandboxManager _sandbox;
    private readonly AgentWorktreeSandbox? _worktree;
    private readonly AgentVerifyEpochTracker _epoch;
    private readonly AgentRoslynL0Diagnostics _l0;
    private readonly AgentOrchestrator _orchestrator;
    private readonly AgentIdleUserTracker _idleUser;
    private readonly AgentAeeRoslynBridge _roslynBridge;
    private readonly object _hostGate = new();
    private readonly object _gate = new();
    private AgentEnvironmentRun? _active;
    private AgentEnvironmentLastRunSummary? _last;
    private CancellationTokenSource? _activeCts;
    private AgentSandboxLease? _activeLease;

    public AgentEnvironmentService(
        IDataBus dataBus,
        AgentEnvironmentSettings settings,
        BuildTestJobService? buildTestJobService = null,
        CSharpLanguageService? languageService = null,
        Func<IReadOnlyList<(string Path, string Content)>>? openCsDocuments = null,
        IGitCommandRunner? gitRunner = null,
        Func<string?>? getWorkspaceRootForL0Git = null,
        Func<string?>? getSolutionPathForOrchestrator = null,
        Func<IReadOnlyList<string>>? getWarmupCsFilePaths = null)
    {
        _dataBus = dataBus;
        _settings = settings;
        _gitRunner = gitRunner;
        _getWorkspaceRootForSnapshot = getWorkspaceRootForL0Git;
        _sandbox = new AgentSandboxManager();
        _epoch = new AgentVerifyEpochTracker(dataBus);
        _coordinator = buildTestJobService?.Coordinator ?? new BuildTestJobCoordinator();
        _buildTestHost = BuildTestHostFactory.Create(settings, _coordinator);
        _l0 = new AgentRoslynL0Diagnostics(
            languageService,
            openCsDocuments,
            settings.Ladder,
            gitRunner,
            getWorkspaceRootForL0Git,
            getWarmupCsFilePaths);
        _ladder = CreateLadder();
        _worktree = gitRunner is null ? null : new AgentWorktreeSandbox(gitRunner);
        _orchestrator = new AgentOrchestrator(
            this,
            settings,
            getSolutionPathForOrchestrator ?? (() => null),
            () => AgentVerifyPolicyParser.TryParse(_settings.DefaultVerifyPolicy, out var p)
                ? p
                : AgentVerifyPolicy.Standard);
        _idleUser = new AgentIdleUserTracker();
        _roslynBridge = new AgentAeeRoslynBridge(languageService);

        _dataBus.Subscribe<AgentEnvironmentTaskDied>(_ => TryRestartBuildTestHostAfterDied());
    }

    public AgentVerifyEpochTracker EpochTracker => _epoch;

    public IAgentOrchestrator Orchestrator => _orchestrator;

    public AgentAeeRoslynBridge RoslynBridge => _roslynBridge;

    public void NotifyCideWindowFocus(bool focused) => _idleUser.NotifyCideFocus(focused);

    public AgentEnvironmentStatusSnapshot GetStatus()
    {
        lock (_gate)
        {
            if (_active is null)
                return new AgentEnvironmentStatusSnapshot(false, null, null, null, null);

            return new AgentEnvironmentStatusSnapshot(
                true,
                _active.RunId,
                _active.VerifySnapshotId,
                _active.PolicyWire,
                _active.SandboxWire,
                WritesInvalidatedVerifyEpoch: _epoch.WritesInvalidatedVerifyEpoch,
                SandboxRunDirectory: _activeLease?.RunDirectory,
                ExecutionChannel: _buildTestHost.HostKind,
                SolutionPath: _active.SolutionPath);
        }
    }

    public AgentSandboxPrepareResult PrepareSandbox(AgentSandboxProfile profile, string? workspaceRoot = null)
    {
        var runId = Guid.NewGuid().ToString("N");
        try
        {
            if (profile == AgentSandboxProfile.AgentWorktree && _worktree is not null
                && !string.IsNullOrWhiteSpace(workspaceRoot))
            {
                var wt = _worktree.TryCreateAsync(workspaceRoot, runId).GetAwaiter().GetResult();
                if (!wt.Success)
                    return new(false, null, wt.Error);

                var lease = _sandbox.Prepare(runId, profile, workspaceRoot);
                return new(true, lease.RunDirectory, $"worktree: {wt.WorktreePath}");
            }

            var leaseOnly = _sandbox.Prepare(runId, profile, workspaceRoot);
            return new(true, leaseOnly.RunDirectory, AgentSandboxProfileParser.ToWire(profile));
        }
        catch (Exception ex)
        {
            return new(false, null, ex.Message);
        }
    }

    public AgentVerifyStartResult StartVerify(
        string solutionPath,
        AgentVerifyPolicy policy,
        AgentSandboxProfile? sandboxProfile = null)
    {
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return new AgentVerifyStartResult(false, null, null, "Solution path is missing or not found.");

        var profile = sandboxProfile ?? ResolveDefaultSandboxProfile();

        lock (_gate)
        {
            CancelActiveCore("superseded");
            var runId = Guid.NewGuid().ToString("N");
            var snapshotId = VerifySnapshot.Create(solutionPath, _gitRunner, _getWorkspaceRootForSnapshot?.Invoke());
            _activeCts = new CancellationTokenSource();
            _activeLease = _sandbox.Prepare(runId, profile);
            _active = new AgentEnvironmentRun(
                runId,
                snapshotId,
                AgentVerifyPolicyParser.ToWire(policy),
                solutionPath,
                AgentSandboxProfileParser.ToWire(profile));
            _epoch.Begin(runId, snapshotId, solutionPath);
            _epoch.WatchPath(solutionPath);
        }

        AgentEnvironmentRun run;
        AgentSandboxLease lease;
        CancellationToken ct;
        lock (_gate)
        {
            run = _active!;
            lease = _activeLease!;
            ct = _activeCts!.Token;
        }

        _dataBus.Publish(new AgentRunStarted(
            run.RunId,
            run.VerifySnapshotId,
            run.PolicyWire,
            run.SolutionPath));
        _dataBus.Publish(new AgentRunPhaseChanged(run.RunId, AgentRunPhaseKind.Environment));

        _ = Task.Run(() => ExecuteVerifyAsync(run, policy, lease, ct), ct);

        return new AgentVerifyStartResult(true, run.RunId, run.VerifySnapshotId, null);
    }

    public AgentVerifyStartResult StartVerifyBatch(AgentVerifyBatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SolutionPath))
            return new(false, null, null, "solution_path is required for batch verify.");

        var profile = request.UseWorktree
            ? AgentSandboxProfile.AgentWorktree
            : request.SandboxProfile;

        return StartVerify(request.SolutionPath, request.Policy, profile);
    }

    public bool CancelActive()
    {
        lock (_gate)
            return CancelActiveCore("cancel");
    }

    public AgentEnvironmentLastRunSummary? GetLastRun()
    {
        lock (_gate)
            return _last;
    }

    private AgentSandboxProfile ResolveDefaultSandboxProfile()
    {
        return AgentSandboxProfileParser.TryParse(_settings.DefaultSandboxProfile, out var p)
            ? p
            : AgentSandboxProfile.AgentEphemeral;
    }

    private bool CancelActiveCore(string? staleReason)
    {
        if (_active is null)
            return false;

        var runId = _active.RunId;
        _ladder.Runner.CancelJobsForRun(runId);

        _activeCts?.Cancel();
        if (staleReason is not null)
            _epoch.End(staleReason);
        else
            _epoch.End();

        _active = null;
        _activeLease = null;
        _activeCts?.Dispose();
        _activeCts = null;
        return true;
    }

    private async Task ExecuteVerifyAsync(
        AgentEnvironmentRun run,
        AgentVerifyPolicy policy,
        AgentSandboxLease lease,
        CancellationToken cancellationToken)
    {
        try
        {
            var threshold = _settings.TimeAccounting.IdleUserThresholdMs;
            if (threshold > 0)
                _idleUser.SampleWhileUnfocused(threshold);

            var result = await _ladder.ClimbAsync(
                run.RunId,
                run.SolutionPath,
                policy,
                lease,
                cancellationToken).ConfigureAwait(false);

            var slices = result.Slices.ToList();
            foreach (var idle in _idleUser.DrainSlices())
                slices.Add(idle);

            _dataBus.Publish(new AgentRunCompleted(
                run.RunId,
                result.Green,
                result.MaxRungReached,
                slices));

            lock (_gate)
            {
                _last = new AgentEnvironmentLastRunSummary(
                    run.RunId,
                    run.VerifySnapshotId,
                    result.Green,
                    result.MaxRungReached,
                    slices,
                    DateTimeOffset.UtcNow);
                if (_active?.RunId == run.RunId)
                {
                    _active = null;
                    _activeLease = null;
                    _activeCts?.Dispose();
                    _activeCts = null;
                    _epoch.End();
                }
            }
        }
        catch (OperationCanceledException)
        {
            _dataBus.Publish(new AgentRunCompleted(run.RunId, false, "cancelled", []));
            lock (_gate)
            {
                if (_active?.RunId == run.RunId)
                    CancelActiveCore("cancel");
            }
        }
    }

    private VerificationLadder CreateLadder() =>
        new(
            _dataBus,
            _buildTestHost,
            _l0,
            _settings,
            _sandbox,
            _gitRunner,
            _getWorkspaceRootForSnapshot);

    private void TryRestartBuildTestHostAfterDied()
    {
        if (!IsSupervisedWorkerHost(_settings.BuildVerifyHost))
            return;

        lock (_hostGate)
        {
            _buildTestHost.MarkUnhealthy();
            _buildTestHost = BuildTestHostFactory.Create(_settings, _coordinator);
            _ladder = CreateLadder();
            _buildTestHost.MarkHealthy();
        }
    }

    private static bool IsSupervisedWorkerHost(string? hostKind)
    {
        var h = hostKind?.Trim() ?? "";
        return string.Equals(h, BuildTestHostFactory.WorkerProcessHostKind, StringComparison.OrdinalIgnoreCase)
            || string.Equals(h, BuildTestHostFactory.WorkerDaemonHostKind, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record AgentEnvironmentRun(
    string RunId,
    string VerifySnapshotId,
    string PolicyWire,
    string SolutionPath,
    string SandboxWire);

public sealed record AgentVerifyStartResult(
    bool Accepted,
    string? RunId,
    string? VerifySnapshotId,
    string? Error);

public sealed record AgentSandboxPrepareResult(
    bool Success,
    string? Path,
    string? Detail);

public sealed record AgentEnvironmentStatusSnapshot(
    bool IsActive,
    string? RunId,
    string? VerifySnapshotId,
    string? Policy,
    string? SandboxProfile,
    bool WritesInvalidatedVerifyEpoch = false,
    string? SandboxRunDirectory = null,
    string ExecutionChannel = "supervised-inproc",
    string? SolutionPath = null)
{
    public AgentEnvironmentStatusSnapshot(bool isActive, string? runId, string? verifySnapshotId, string? policy)
        : this(isActive, runId, verifySnapshotId, policy, null, false, null, "supervised-inproc")
    {
    }

    public AgentEnvironmentStatusSnapshot(bool isActive, string? runId, string? verifySnapshotId, string? policy, string? sandboxProfile)
        : this(isActive, runId, verifySnapshotId, policy, sandboxProfile, false, null, "supervised-inproc")
    {
    }
}

public sealed record AgentEnvironmentLastRunSummary(
    string RunId,
    string VerifySnapshotId,
    bool Green,
    string MaxRungReached,
    IReadOnlyList<AgentTimeSlice> TimeSlices,
    DateTimeOffset CompletedAtUtc)
{
    public string FormatChatTrace()
    {
        var envLine = TimeSlices.FirstOrDefault(s => s.Phase == AgentRunPhaseKind.Environment);
        var envText = envLine is null
            ? "—"
            : $"{envLine.DurationSeconds:0.0}s ({envLine.Detail ?? "environment"})";

        return $"""
            Agent verify {RunId[..8]}…
              Environment: {envText}
              Policy snapshot: {VerifySnapshotId}
              Status: {(Green ? "green" : "failed")} ({MaxRungReached})
            """;
    }
}

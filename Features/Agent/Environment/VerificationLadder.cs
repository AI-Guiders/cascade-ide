using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Environment;

public sealed record VerificationLadderResult(
    bool Green,
    string MaxRungReached,
    IReadOnlyList<AgentTimeSlice> Slices,
    string? FailureDetail);

/// <summary>Verify rung climb (ADR 0148 W2).</summary>
public sealed class VerificationLadder
{
    private readonly EnvironmentTaskRunner _runner;
    private readonly AgentRoslynDiagnoseFilesDiagnostics _diagnoseFiles;
    private readonly AgentEnvironmentSettings _settings;
    private readonly AgentSandboxManager _sandbox;
    private readonly EnvironmentTaskDedup _dedup;
    private readonly IDataBus _dataBus;
    private readonly IGitCommandRunner? _gitRunner;
    private readonly Func<string?>? _getWorkspaceRoot;

    public VerificationLadder(
        IDataBus dataBus,
        IBuildTestHost host,
        AgentRoslynDiagnoseFilesDiagnostics diagnoseFiles,
        AgentEnvironmentSettings settings,
        AgentSandboxManager sandbox,
        IGitCommandRunner? gitRunner = null,
        Func<string?>? getWorkspaceRoot = null)
    {
        _dataBus = dataBus;
        _runner = new EnvironmentTaskRunner(dataBus, host.JobBackend);
        _diagnoseFiles = diagnoseFiles;
        _settings = settings;
        _sandbox = sandbox;
        _dedup = new EnvironmentTaskDedup(settings.CoalesceWindowMs);
        _gitRunner = gitRunner;
        _getWorkspaceRoot = getWorkspaceRoot;
    }

    public EnvironmentTaskRunner Runner => _runner;

    public async Task<VerificationLadderResult> ClimbAsync(
        string runId,
        string solutionPath,
        AgentVerifyPolicy policy,
        AgentSandboxLease sandboxLease,
        CancellationToken cancellationToken)
    {
        var slices = new List<AgentTimeSlice>();
        var envStart = DateTimeOffset.UtcNow;
        var maxRung = VerifyRung.DiagnoseFiles;
        var green = true;
        string? failure = null;

        try
        {
            if (policy != AgentVerifyPolicy.Minimal && _settings.Ladder.DiagnoseFilesEnabled)
            {
                maxRung = VerifyRung.DiagnoseFiles;
                var diagnoseStart = DateTimeOffset.UtcNow;
                var diagnose = await _diagnoseFiles.RunAsync(cancellationToken).ConfigureAwait(false);
                slices.Add(new AgentTimeSlice(
                    AgentRunPhaseKind.Environment,
                    (DateTimeOffset.UtcNow - diagnoseStart).TotalSeconds,
                    diagnose.Detail));
                green = diagnose.Green;
                if (!green)
                {
                    failure = diagnose.Detail;
                    return Finish(slices, envStart, maxRung, green, failure);
                }
            }

            if (green && policy is not AgentVerifyPolicy.Minimal)
            {
                maxRung = VerifyRung.CompileProject;
                slices.Add(new AgentTimeSlice(
                    AgentRunPhaseKind.Environment,
                    0,
                    $"{VerifyRung.CompileProject}: delegated to {VerifyRung.BuildAffected} (MLP)"));
            }

            if (green)
            {
                var dedupKey = $"build|{solutionPath}";
                var coalesced = _dedup.ShouldCoalesce(dedupKey);
                if (!coalesced)
                {
                    maxRung = VerifyRung.BuildAffected;
                    var build = await _runner.RunBuildAsync(
                            runId,
                            solutionPath,
                            waitForCompletion: true,
                            cancellationToken)
                        .ConfigureAwait(false);
                    green = build.Success;
                    if (!green)
                        failure = build.Status;
                }
                else
                {
                    maxRung = VerifyRung.BuildAffected;
                    slices.Add(new AgentTimeSlice(
                        AgentRunPhaseKind.Environment,
                        0,
                        $"{VerifyRung.BuildAffected}: build skipped (dedup/coalesce window; prior build assumed sufficient)"));
                }
            }

            if (green && policy is AgentVerifyPolicy.Standard or AgentVerifyPolicy.Strict or AgentVerifyPolicy.CiParity)
            {
                sandboxLease = sandboxLease with
                {
                    Substrate = _sandbox.RecreateSubstrateBeforeTests(sandboxLease),
                };
                maxRung = VerifyRung.TestScoped;

                var contract = AgentDevServiceContractValidator.ValidateForTestScoped(
                    _settings.DevServices,
                    sandboxLease.Profile,
                    sandboxLease);
                slices.Add(new AgentTimeSlice(AgentRunPhaseKind.Environment, 0, contract.Detail));
                if (!contract.Ok && _settings.DevServices.GateTestScopedOnViolation)
                {
                    green = false;
                    failure = contract.Detail;
                    return Finish(slices, envStart, maxRung, green, failure);
                }

                var supplementalEnv = sandboxLease.Substrate is null
                    ? null
                    : AgentSandboxProcessEnvironmentKeys.ForBundle(sandboxLease.Substrate);

                var filter = await AgentTestScopedTouchedTestFilter.BuildFilterExpressionAsync(
                    _settings.Ladder,
                    _gitRunner,
                    _getWorkspaceRoot?.Invoke(),
                    cancellationToken).ConfigureAwait(false);

                var test = await _runner.RunTestsAsync(
                    runId,
                    solutionPath,
                    filterExpression: filter,
                    waitForCompletion: true,
                    cancellationToken,
                    supplementalEnvironmentVariables: supplementalEnv).ConfigureAwait(false);
                green = test.Success;
                if (!green)
                    failure = test.Status;
            }

            if (green && policy is AgentVerifyPolicy.CiParity)
            {
                maxRung = VerifyRung.TestFull;
                if (_settings.Ladder.TestFullRequireExplicit)
                    slices.Add(new AgentTimeSlice(
                        AgentRunPhaseKind.Environment,
                        0,
                        $"{VerifyRung.TestFull}: ci_parity marker (MLP)"));
            }
        }
        catch (OperationCanceledException)
        {
            green = false;
            failure = "cancelled";
        }

        return Finish(slices, envStart, maxRung, green, failure);
    }

    private VerificationLadderResult Finish(
        List<AgentTimeSlice> slices,
        DateTimeOffset envStart,
        string maxRung,
        bool green,
        string? failure)
    {
        var total = (DateTimeOffset.UtcNow - envStart).TotalSeconds;
        if (!slices.Any(s => s.Detail?.StartsWith(VerifyRung.DiagnoseFiles, StringComparison.Ordinal) == true))
            slices.Insert(0, new AgentTimeSlice(AgentRunPhaseKind.Environment, total, $"ladder → {maxRung}"));
        return new(green, maxRung, slices, failure);
    }
}

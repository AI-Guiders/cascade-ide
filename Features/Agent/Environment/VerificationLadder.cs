using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

public sealed record VerificationLadderResult(
    bool Green,
    string MaxRungReached,
    IReadOnlyList<AgentTimeSlice> Slices,
    string? FailureDetail);

/// <summary>L0–L4 climb (ADR 0148 W2).</summary>
public sealed class VerificationLadder
{
    private readonly EnvironmentTaskRunner _runner;
    private readonly AgentRoslynL0Diagnostics _l0;
    private readonly AgentEnvironmentSettings _settings;
    private readonly AgentSandboxManager _sandbox;
    private readonly EnvironmentTaskDedup _dedup;
    private readonly IDataBus _dataBus;

    public VerificationLadder(
        IDataBus dataBus,
        IBuildTestHost host,
        AgentRoslynL0Diagnostics l0,
        AgentEnvironmentSettings settings,
        AgentSandboxManager sandbox)
    {
        _dataBus = dataBus;
        _runner = new EnvironmentTaskRunner(dataBus, host.Coordinator);
        _l0 = l0;
        _settings = settings;
        _sandbox = sandbox;
        _dedup = new EnvironmentTaskDedup(settings.CoalesceWindowMs);
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
        var maxRung = "L0";
        var green = true;
        string? failure = null;

        try
        {
            if (policy != AgentVerifyPolicy.Minimal && _settings.Ladder.L0Enabled)
            {
                maxRung = "L0";
                var l0Start = DateTimeOffset.UtcNow;
                var l0 = await _l0.RunAsync(cancellationToken).ConfigureAwait(false);
                slices.Add(new AgentTimeSlice(
                    AgentRunPhaseKind.Environment,
                    (DateTimeOffset.UtcNow - l0Start).TotalSeconds,
                    l0.Detail));
                green = l0.Green;
                if (!green)
                {
                    failure = l0.Detail;
                    return Finish(slices, envStart, maxRung, green, failure);
                }
            }

            if (green && policy is not AgentVerifyPolicy.Minimal)
            {
                maxRung = "L1";
                slices.Add(new AgentTimeSlice(AgentRunPhaseKind.Environment, 0, "L1: delegated to L2 (MLP)"));
            }

            if (green)
            {
                var dedupKey = $"build|{solutionPath}";
                if (!_dedup.ShouldCoalesce(dedupKey))
                {
                    maxRung = "L2";
                    var build = await _runner.RunBuildAsync(
                        runId,
                        solutionPath,
                        waitForCompletion: true,
                        cancellationToken).ConfigureAwait(false);
                    green = build.Success;
                    if (!green)
                        failure = build.Status;
                }
            }

            if (green && policy is AgentVerifyPolicy.Standard or AgentVerifyPolicy.Strict or AgentVerifyPolicy.CiParity)
            {
                _sandbox.RecreateSubstrateBeforeTests(sandboxLease);
                maxRung = "L3";
                var test = await _runner.RunTestsAsync(
                    runId,
                    solutionPath,
                    filterExpression: null,
                    waitForCompletion: true,
                    cancellationToken).ConfigureAwait(false);
                green = test.Success;
                if (!green)
                    failure = test.Status;
            }

            if (green && policy is AgentVerifyPolicy.CiParity)
            {
                maxRung = "L4";
                if (_settings.Ladder.L4RequireExplicit)
                    slices.Add(new AgentTimeSlice(AgentRunPhaseKind.Environment, 0, "L4: ci_parity marker (MLP)"));
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
        if (!slices.Any(s => s.Detail?.StartsWith("L0", StringComparison.Ordinal) == true))
            slices.Insert(0, new AgentTimeSlice(AgentRunPhaseKind.Environment, total, $"ladder → {maxRung}"));
        return new(green, maxRung, slices, failure);
    }
}

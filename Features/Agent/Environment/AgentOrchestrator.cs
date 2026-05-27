using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Оркестрация verify: policy, sandbox profile, worktree (ADR 0148 W6/E).</summary>
public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAgentEnvironmentService _environment;
    private readonly AgentEnvironmentSettings _settings;
    private readonly Func<string?> _getSolutionPath;
    private readonly Func<AgentVerifyPolicy> _getDefaultPolicy;

    public AgentOrchestrator(
        IAgentEnvironmentService environment,
        AgentEnvironmentSettings settings,
        Func<string?> getSolutionPath,
        Func<AgentVerifyPolicy>? getDefaultPolicy = null)
    {
        _environment = environment;
        _settings = settings;
        _getSolutionPath = getSolutionPath;
        _getDefaultPolicy = getDefaultPolicy ?? (() => AgentVerifyPolicy.Standard);
    }

    public AgentVerifyStartResult TryVerifyAfterStep(bool writesOccurred, bool longRun = false)
    {
        if (!writesOccurred)
            return new(false, null, null, "No writes; verify skipped.");

        var solution = _getSolutionPath();
        if (string.IsNullOrWhiteSpace(solution))
            return new(false, null, null, "Solution not open.");

        if (longRun)
            return StartLongRunVerify(solution);

        return _environment.StartVerify(solution, _getDefaultPolicy());
    }

    private AgentVerifyStartResult StartLongRunVerify(string solution)
    {
        var profile = ResolveLongRunSandboxProfile();
        var useWorktree = profile == AgentSandboxProfile.AgentWorktree;

        return _environment.StartVerifyBatch(new AgentVerifyBatchRequest(
            _getDefaultPolicy(),
            profile,
            solution,
            UseWorktree: useWorktree));
    }

    private AgentSandboxProfile ResolveLongRunSandboxProfile()
    {
        if (AgentSandboxProfileParser.TryParse(_settings.LongRunSandboxProfile, out var profile))
            return profile;

        return AgentSandboxProfile.AgentWorktree;
    }
}

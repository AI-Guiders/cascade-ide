namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Hook for autonomous agent: verify after tool batch (ADR 0148 W6 minimal).</summary>
public sealed class AgentOrchestratorBridge
{
    private readonly IAgentEnvironmentService _environment;
    private readonly Func<string?> _getSolutionPath;
    private readonly Func<AgentVerifyPolicy> _getDefaultPolicy;

    public AgentOrchestratorBridge(
        IAgentEnvironmentService environment,
        Func<string?> getSolutionPath,
        Func<AgentVerifyPolicy>? getDefaultPolicy = null)
    {
        _environment = environment;
        _getSolutionPath = getSolutionPath;
        _getDefaultPolicy = getDefaultPolicy ?? (() => AgentVerifyPolicy.Standard);
    }

    public AgentVerifyStartResult TryVerifyAfterStep(bool writesOccurred)
    {
        if (!writesOccurred)
            return new(false, null, null, "No writes; verify skipped.");

        var solution = _getSolutionPath();
        if (string.IsNullOrWhiteSpace(solution))
            return new(false, null, null, "Solution not open.");

        return _environment.StartVerify(solution, _getDefaultPolicy());
    }
}

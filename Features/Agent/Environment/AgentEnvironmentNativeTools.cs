namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Batch native AEE tools (ADR 0148 W6).</summary>
public static class AgentEnvironmentNativeTools
{
    public static IReadOnlyList<string> ToolIds { get; } =
    [
        "ide_agent_verify",
        "ide_agent_cancel",
        "ide_agent_status",
        "ide_agent_last",
        "ide_agent_sandbox_prepare",
    ];

    public static AgentVerifyBatchRequest ParseBatchVerify(IReadOnlyDictionary<string, string?> args)
    {
        var policy = AgentVerifyPolicy.Standard;
        if (args.TryGetValue("policy", out var p) && AgentVerifyPolicyParser.TryParse(p, out var parsed))
            policy = parsed;

        AgentSandboxProfile sandbox = AgentSandboxProfile.AgentEphemeral;
        if (args.TryGetValue("sandbox_profile", out var s) && AgentSandboxProfileParser.TryParse(s, out var sp))
            sandbox = sp;

        return new AgentVerifyBatchRequest(policy, sandbox, args.GetValueOrDefault("solution_path"));
    }
}

public sealed record AgentVerifyBatchRequest(
    AgentVerifyPolicy Policy,
    AgentSandboxProfile SandboxProfile,
    string? SolutionPath);

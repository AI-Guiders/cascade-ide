using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

public sealed record DevServiceContractCheckResult(bool Ok, string Detail);

/// <summary>Проверка, что ephemeral substrate задаёт override для dev DB (ADR 0148 §6).</summary>
public static class AgentDevServiceContractValidator
{
    private static readonly string[] s_requiredEnvKeys =
    [
        AgentSandboxProcessEnvironmentKeys.IntercomDataDirectory,
        AgentSandboxProcessEnvironmentKeys.WitDbPath,
        AgentSandboxProcessEnvironmentKeys.DevPort,
    ];

    public static DevServiceContractCheckResult ValidateForTestScoped(
        AgentDevServiceContractSettings contract,
        AgentSandboxProfile profile,
        AgentSandboxLease lease)
    {
        if (!contract.RequireConfigOverride)
            return new(true, "dev contract: override not required");

        if (profile != AgentSandboxProfile.AgentEphemeral)
            return new(true, "dev contract: skipped (non-ephemeral profile)");

        if (lease.Substrate is null)
            return contract.GateTestScopedOnViolation
                ? new(false, "dev contract: ephemeral run without substrate bundle")
                : new(true, $"dev contract: warn — no substrate ({VerifyRung.TestScoped} not gated)");

        var env = AgentSandboxProcessEnvironmentKeys.ForBundle(lease.Substrate);
        foreach (var key in s_requiredEnvKeys)
        {
            if (!env.ContainsKey(key) || string.IsNullOrWhiteSpace(env[key]))
                return new(false, $"dev contract: missing env override '{key}'");
        }

        return new(true, "dev contract: ok");
    }
}

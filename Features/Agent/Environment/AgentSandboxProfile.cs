namespace CascadeIDE.Features.Agent.Environment;

public enum AgentSandboxProfile
{
    AgentEphemeral,
    AgentWorktree,
    InPlace,
}

public static class AgentSandboxProfileParser
{
    public static bool TryParse(string? raw, out AgentSandboxProfile profile)
    {
        profile = AgentSandboxProfile.AgentEphemeral;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        return raw.Trim().ToLowerInvariant() switch
        {
            "agent_ephemeral" or "ephemeral" => Assign(AgentSandboxProfile.AgentEphemeral, out profile),
            "agent_worktree" or "worktree" => Assign(AgentSandboxProfile.AgentWorktree, out profile),
            "in_place" or "inplace" => Assign(AgentSandboxProfile.InPlace, out profile),
            _ => false,
        };
    }

    private static bool Assign(AgentSandboxProfile value, out AgentSandboxProfile profile)
    {
        profile = value;
        return true;
    }

    public static string ToWire(AgentSandboxProfile profile) => profile switch
    {
        AgentSandboxProfile.AgentWorktree => "agent_worktree",
        AgentSandboxProfile.InPlace => "in_place",
        _ => "agent_ephemeral",
    };
}

namespace CascadeIDE.Features.Agent.Environment;

public enum AgentVerifyPolicy
{
    Minimal,
    Standard,
    Strict,
    CiParity,
}

public static class AgentVerifyPolicyParser
{
    public static bool TryParse(string? raw, out AgentVerifyPolicy policy)
    {
        policy = AgentVerifyPolicy.Standard;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        return raw.Trim().ToLowerInvariant() switch
        {
            "minimal" or "quick" => Assign(AgentVerifyPolicy.Minimal, out policy),
            "standard" => Assign(AgentVerifyPolicy.Standard, out policy),
            "strict" => Assign(AgentVerifyPolicy.Strict, out policy),
            "ci" or "ci_parity" or "full" => Assign(AgentVerifyPolicy.CiParity, out policy),
            _ => false,
        };
    }

    private static bool Assign(AgentVerifyPolicy value, out AgentVerifyPolicy policy)
    {
        policy = value;
        return true;
    }

    public static string ToWire(AgentVerifyPolicy policy) => policy switch
    {
        AgentVerifyPolicy.Minimal => "minimal",
        AgentVerifyPolicy.Standard => "standard",
        AgentVerifyPolicy.Strict => "strict",
        AgentVerifyPolicy.CiParity => "ci_parity",
        _ => "standard",
    };
}

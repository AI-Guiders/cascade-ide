namespace CascadeIDE.Features.Agent.Environment;

/// <summary><c>[agent.environment] shell_escape_tier</c> (ADR 0148 · naming-layers-v1 §7).</summary>
public static class ShellEscapeTier
{
    public const string Deny = "deny";

    /// <summary>Only direct test MCP commands; build/format via ide_agent_verify.</summary>
    public const string TestsOnly = "tests_only";

    public const string AllowWithAudit = "allow_with_audit";
}

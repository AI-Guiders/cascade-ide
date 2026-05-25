namespace CascadeIDE.Models;

/// <summary>AEE policy and runner (ADR 0148 §11). TOML: <c>[agent.environment]</c>.</summary>
public sealed class AgentEnvironmentSettings
{
    public string DefaultVerifyPolicy { get; set; } = "standard";

    public string DefaultSandboxProfile { get; set; } = "agent_ephemeral";

    public int RunnerMaxConcurrency { get; set; } = 2;

    public int CoalesceWindowMs { get; set; } = 1500;

    /// <summary>deny | l3_only | allow_with_audit</summary>
    public string ShellEscapeTier { get; set; } = "deny";

    public AgentEnvironmentLadderSettings Ladder { get; set; } = new();

    public AgentEnvironmentTimeAccountingSettings TimeAccounting { get; set; } = new();
}

public sealed class AgentEnvironmentLadderSettings
{
    public bool L0Enabled { get; set; } = true;

    public bool L4RequireExplicit { get; set; } = true;
}

public sealed class AgentEnvironmentTimeAccountingSettings
{
    public bool ShowInChat { get; set; } = true;

    public bool PfdInstrumentEnabled { get; set; } = true;
}

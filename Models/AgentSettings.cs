namespace CascadeIDE.Models;

/// <summary>Agent-facing IDE settings. TOML: <c>[agent.*]</c>.</summary>
public sealed class AgentSettings
{
    public AgentEnvironmentSettings Environment { get; set; } = new();
}

namespace CascadeIDE.Models;

/// <summary>Контракт dev-сервисов для <c>agent_ephemeral</c> (ADR 0148 §6).</summary>
public sealed class AgentDevServiceContractSettings
{
    /// <summary>Требовать известные env override перед test.scoped.</summary>
    public bool RequireConfigOverride { get; set; } = true;

    /// <summary>Блокировать test.scoped при нарушении контракта (иначе только audit slice).</summary>
    public bool GateTestScopedOnViolation { get; set; } = true;
}

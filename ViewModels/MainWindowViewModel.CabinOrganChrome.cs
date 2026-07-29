#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Dual-cockpit cabin packing + L1 pressure / AutoIgnition continuity chrome.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCabinChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentCabinChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPressureChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentPressureChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentIgniteChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentIgniteChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentScopeChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentScopeChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentSysChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentSysChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentOnboardChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentOnboardChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentArchChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentArchChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentMcpChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentMcpChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPlanChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentPlanChromeHint;

    public bool ShowAgentCabinChromeHint => !string.IsNullOrWhiteSpace(AgentCabinChromeHint);

    public bool ShowAgentPressureChromeHint => !string.IsNullOrWhiteSpace(AgentPressureChromeHint);

    public bool ShowAgentIgniteChromeHint => !string.IsNullOrWhiteSpace(AgentIgniteChromeHint);

    public bool ShowAgentScopeChromeHint => !string.IsNullOrWhiteSpace(AgentScopeChromeHint);

    public bool ShowAgentSysChromeHint => !string.IsNullOrWhiteSpace(AgentSysChromeHint);

    public bool ShowAgentOnboardChromeHint => !string.IsNullOrWhiteSpace(AgentOnboardChromeHint);

    public bool ShowAgentArchChromeHint => !string.IsNullOrWhiteSpace(AgentArchChromeHint);

    public bool ShowAgentMcpChromeHint => !string.IsNullOrWhiteSpace(AgentMcpChromeHint);

    public bool ShowAgentPlanChromeHint => !string.IsNullOrWhiteSpace(AgentPlanChromeHint);

    /// <summary>Apply seats-latch chrome_hint (Dark Cockpit — only when non-empty).</summary>
    public void ApplyCabinOrganChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentCabinChromeHint, next, StringComparison.Ordinal))
            return;
        AgentCabinChromeHint = next;
    }

    /// <summary>Apply pressure-LATEST chrome_hint (L1 armed pulse; idle clears).</summary>
    public void ApplyPressureChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentPressureChromeHint, next, StringComparison.Ordinal))
            return;
        AgentPressureChromeHint = next;
    }

    /// <summary>Apply ignite-LATEST chrome_hint (continuity live; idle clears).</summary>
    public void ApplyIgniteChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentIgniteChromeHint, next, StringComparison.Ordinal))
            return;
        AgentIgniteChromeHint = next;
    }

    /// <summary>Apply scope-LATEST chrome_hint (PRIMARY/SCOPE; empty clears).</summary>
    public void ApplyScopeChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentScopeChromeHint, next, StringComparison.Ordinal))
            return;
        AgentScopeChromeHint = next;
    }

    /// <summary>Apply sys-LATEST chrome_hint (ops pulse; idle clears).</summary>
    public void ApplySysChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentSysChromeHint, next, StringComparison.Ordinal))
            return;
        AgentSysChromeHint = next;
    }

    /// <summary>Apply onboard-LATEST chrome_hint (cold-start map; empty clears).</summary>
    public void ApplyOnboardChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentOnboardChromeHint, next, StringComparison.Ordinal))
            return;
        AgentOnboardChromeHint = next;
    }

    /// <summary>Apply arch-LATEST chrome_hint (board/as_built; empty clears).</summary>
    public void ApplyArchChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentArchChromeHint, next, StringComparison.Ordinal))
            return;
        AgentArchChromeHint = next;
    }

    /// <summary>Apply mcp-LATEST chrome_hint (outlet guests; idle clears).</summary>
    public void ApplyMcpChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentMcpChromeHint, next, StringComparison.Ordinal))
            return;
        AgentMcpChromeHint = next;
    }

    /// <summary>Apply plan-LATEST chrome_hint (Task Manager focus; empty clears).</summary>
    public void ApplyPlanChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentPlanChromeHint, next, StringComparison.Ordinal))
            return;
        AgentPlanChromeHint = next;
    }
}

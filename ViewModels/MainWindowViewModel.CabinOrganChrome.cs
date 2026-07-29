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

    public bool ShowAgentCabinChromeHint => !string.IsNullOrWhiteSpace(AgentCabinChromeHint);

    public bool ShowAgentPressureChromeHint => !string.IsNullOrWhiteSpace(AgentPressureChromeHint);

    public bool ShowAgentIgniteChromeHint => !string.IsNullOrWhiteSpace(AgentIgniteChromeHint);

    public bool ShowAgentScopeChromeHint => !string.IsNullOrWhiteSpace(AgentScopeChromeHint);

    public bool ShowAgentSysChromeHint => !string.IsNullOrWhiteSpace(AgentSysChromeHint);

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
}

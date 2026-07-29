#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Dual-cockpit cabin packing: quiet chrome hint when agent M organ has no MFD page.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCabinChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentCabinChromeHint;

    public bool ShowAgentCabinChromeHint => !string.IsNullOrWhiteSpace(AgentCabinChromeHint);

    /// <summary>Apply seats-latch chrome_hint (Dark Cockpit — only when non-empty).</summary>
    public void ApplyCabinOrganChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentCabinChromeHint, next, StringComparison.Ordinal))
            return;
        AgentCabinChromeHint = next;
    }
}

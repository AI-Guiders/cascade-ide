#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Dual-cockpit cabin packing + L1 pressure / AutoIgnition continuity chrome hints.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCabinChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentCabinChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPressureChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentPressureChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentIgniteChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentIgniteChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentScopeChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentScopeChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentSysChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentSysChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentOnboardChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentOnboardChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentArchChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentArchChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentMcpChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentMcpChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPlanChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentPlanChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentReportChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentReportChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCrmChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentCrmChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentWebcamChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentWebcamChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentToolchainChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentToolchainChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPluginsChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentPluginsChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentRefactorChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentRefactorChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentReviewChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentReviewChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentLearnChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentLearnChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentDomainChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentDomainChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentSaDeskChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintVisibleLines))]
    [NotifyPropertyChangedFor(nameof(AgentChromeHintOverflow))]
    [NotifyPropertyChangedFor(nameof(ShowAgentChromeHintOverflow))]
    private string? _agentSaDeskChromeHint;


    public void ApplyCabinOrganChromeHint(string? hint) =>
        ApplyChromeHint(AgentCabinChromeHint, hint, v => AgentCabinChromeHint = v);

    public void ApplyPressureChromeHint(string? hint) =>
        ApplyChromeHint(AgentPressureChromeHint, hint, v => AgentPressureChromeHint = v);

    public void ApplyIgniteChromeHint(string? hint) =>
        ApplyChromeHint(AgentIgniteChromeHint, hint, v => AgentIgniteChromeHint = v);

    public void ApplyScopeChromeHint(string? hint) =>
        ApplyChromeHint(AgentScopeChromeHint, hint, v => AgentScopeChromeHint = v);

    public void ApplySysChromeHint(string? hint) =>
        ApplyChromeHint(AgentSysChromeHint, hint, v => AgentSysChromeHint = v);

    public void ApplyOnboardChromeHint(string? hint) =>
        ApplyChromeHint(AgentOnboardChromeHint, hint, v => AgentOnboardChromeHint = v);

    public void ApplyArchChromeHint(string? hint) =>
        ApplyChromeHint(AgentArchChromeHint, hint, v => AgentArchChromeHint = v);

    public void ApplyMcpChromeHint(string? hint) =>
        ApplyChromeHint(AgentMcpChromeHint, hint, v => AgentMcpChromeHint = v);

    public void ApplyPlanChromeHint(string? hint) =>
        ApplyChromeHint(AgentPlanChromeHint, hint, v => AgentPlanChromeHint = v);

    public void ApplyReportChromeHint(string? hint) =>
        ApplyChromeHint(AgentReportChromeHint, hint, v => AgentReportChromeHint = v);

    public void ApplyCrmChromeHint(string? hint) =>
        ApplyChromeHint(AgentCrmChromeHint, hint, v => AgentCrmChromeHint = v);

    public void ApplyWebcamChromeHint(string? hint) =>
        ApplyChromeHint(AgentWebcamChromeHint, hint, v => AgentWebcamChromeHint = v);

    public void ApplyToolchainChromeHint(string? hint) =>
        ApplyChromeHint(AgentToolchainChromeHint, hint, v => AgentToolchainChromeHint = v);

    public void ApplyPluginsChromeHint(string? hint) =>
        ApplyChromeHint(AgentPluginsChromeHint, hint, v => AgentPluginsChromeHint = v);

    public void ApplyRefactorChromeHint(string? hint) =>
        ApplyChromeHint(AgentRefactorChromeHint, hint, v => AgentRefactorChromeHint = v);

    public void ApplyReviewChromeHint(string? hint) =>
        ApplyChromeHint(AgentReviewChromeHint, hint, v => AgentReviewChromeHint = v);

    public void ApplyLearnChromeHint(string? hint) =>
        ApplyChromeHint(AgentLearnChromeHint, hint, v => AgentLearnChromeHint = v);

    public void ApplyDomainChromeHint(string? hint) =>
        ApplyChromeHint(AgentDomainChromeHint, hint, v => AgentDomainChromeHint = v);

    public void ApplySaDeskChromeHint(string? hint) =>
        ApplyChromeHint(AgentSaDeskChromeHint, hint, v => AgentSaDeskChromeHint = v);

    static void ApplyChromeHint(string? current, string? hint, Action<string?> set)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(current, next, StringComparison.Ordinal)) return;
        set(next);
    }
}

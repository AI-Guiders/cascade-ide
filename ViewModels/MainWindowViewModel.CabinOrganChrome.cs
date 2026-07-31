#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Chrome hint ObservableProperty fields (Apply/Show/density live in SoftOrganChrome).</summary>
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
}

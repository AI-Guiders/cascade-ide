#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Chrome hint ObservableProperty fields (Apply/Show/density live in SoftOrganChrome).</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCabinChromeHint))]
    private string? _agentCabinChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPressureChromeHint))]
    private string? _agentPressureChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentIgniteChromeHint))]
    private string? _agentIgniteChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentScopeChromeHint))]
    private string? _agentScopeChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentSysChromeHint))]
    private string? _agentSysChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentOnboardChromeHint))]
    private string? _agentOnboardChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentArchChromeHint))]
    private string? _agentArchChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentMcpChromeHint))]
    private string? _agentMcpChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPlanChromeHint))]
    private string? _agentPlanChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentReportChromeHint))]
    private string? _agentReportChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCrmChromeHint))]
    private string? _agentCrmChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentWebcamChromeHint))]
    private string? _agentWebcamChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentToolchainChromeHint))]
    private string? _agentToolchainChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentTestDeskChromeHint))]
    private string? _agentTestDeskChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentDebugDeskChromeHint))]
    private string? _agentDebugDeskChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPluginsChromeHint))]
    private string? _agentPluginsChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentRefactorChromeHint))]
    private string? _agentRefactorChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentReviewChromeHint))]
    private string? _agentReviewChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentLearnChromeHint))]
    private string? _agentLearnChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentDomainChromeHint))]
    private string? _agentDomainChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentSaDeskChromeHint))]
    private string? _agentSaDeskChromeHint;
}

#nullable enable
using CascadeIDE.Features.UiChrome;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentReportChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentReportChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentCrmChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentCrmChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentWebcamChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentWebcamChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentToolchainChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentToolchainChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentPluginsChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentPluginsChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentRefactorChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentRefactorChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentReviewChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentReviewChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentLearnChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentLearnChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentDomainChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentDomainChromeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAgentSaDeskChromeHint))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    private string? _agentSaDeskChromeHint;

    public bool ShowAgentCabinChromeHint => !string.IsNullOrWhiteSpace(AgentCabinChromeHint);
    public bool ShowAgentPressureChromeHint => !string.IsNullOrWhiteSpace(AgentPressureChromeHint);
    public bool ShowAgentIgniteChromeHint => !string.IsNullOrWhiteSpace(AgentIgniteChromeHint);
    public bool ShowAgentScopeChromeHint => !string.IsNullOrWhiteSpace(AgentScopeChromeHint);
    public bool ShowAgentSysChromeHint => !string.IsNullOrWhiteSpace(AgentSysChromeHint);
    public bool ShowAgentOnboardChromeHint => !string.IsNullOrWhiteSpace(AgentOnboardChromeHint);
    public bool ShowAgentArchChromeHint => !string.IsNullOrWhiteSpace(AgentArchChromeHint);
    public bool ShowAgentMcpChromeHint => !string.IsNullOrWhiteSpace(AgentMcpChromeHint);
    public bool ShowAgentPlanChromeHint => !string.IsNullOrWhiteSpace(AgentPlanChromeHint);
    public bool ShowAgentReportChromeHint => !string.IsNullOrWhiteSpace(AgentReportChromeHint);
    public bool ShowAgentCrmChromeHint => !string.IsNullOrWhiteSpace(AgentCrmChromeHint);
    public bool ShowAgentWebcamChromeHint => !string.IsNullOrWhiteSpace(AgentWebcamChromeHint);
    public bool ShowAgentToolchainChromeHint => !string.IsNullOrWhiteSpace(AgentToolchainChromeHint);
    public bool ShowAgentPluginsChromeHint => !string.IsNullOrWhiteSpace(AgentPluginsChromeHint);
    public bool ShowAgentRefactorChromeHint => !string.IsNullOrWhiteSpace(AgentRefactorChromeHint);
    public bool ShowAgentReviewChromeHint => !string.IsNullOrWhiteSpace(AgentReviewChromeHint);
    public bool ShowAgentLearnChromeHint => !string.IsNullOrWhiteSpace(AgentLearnChromeHint);
    public bool ShowAgentDomainChromeHint => !string.IsNullOrWhiteSpace(AgentDomainChromeHint);
    public bool ShowAgentSaDeskChromeHint => !string.IsNullOrWhiteSpace(AgentSaDeskChromeHint);

    public IReadOnlyList<string> AgentChromeHintVisibleLines => BuildChromeHintDensity().VisibleLines;
    public string? AgentChromeHintOverflow => BuildChromeHintDensity().OverflowLine;
    public bool ShowAgentChromeHintOverflow => !string.IsNullOrWhiteSpace(AgentChromeHintOverflow);

    AgentChromeHintDensityPolicy.Result BuildChromeHintDensity() =>
        AgentChromeHintDensityPolicy.Collapse(CollectChromeHintCandidates());

    IEnumerable<AgentChromeHintDensityPolicy.Hint> CollectChromeHintCandidates()
    {
        if (AgentChromeHintDensityPolicy.From("pressure", AgentPressureChromeHint) is { } p) yield return p;
        if (AgentChromeHintDensityPolicy.From("ignite", AgentIgniteChromeHint) is { } i) yield return i;
        if (AgentChromeHintDensityPolicy.From("plan", AgentPlanChromeHint) is { } pl) yield return pl;
        if (AgentChromeHintDensityPolicy.From("cabin", AgentCabinChromeHint) is { } c) yield return c;
        if (AgentChromeHintDensityPolicy.From("scope", AgentScopeChromeHint) is { } s) yield return s;
        if (AgentChromeHintDensityPolicy.From("review", AgentReviewChromeHint) is { } r) yield return r;
        if (AgentChromeHintDensityPolicy.From("refactor", AgentRefactorChromeHint) is { } rf) yield return rf;
        if (AgentChromeHintDensityPolicy.From("plugins", AgentPluginsChromeHint) is { } pg) yield return pg;
        if (AgentChromeHintDensityPolicy.From("toolchain", AgentToolchainChromeHint) is { } t) yield return t;
        if (AgentChromeHintDensityPolicy.From("crm", AgentCrmChromeHint) is { } crm) yield return crm;
        if (AgentChromeHintDensityPolicy.From("report", AgentReportChromeHint) is { } rp) yield return rp;
        if (AgentChromeHintDensityPolicy.From("webcam", AgentWebcamChromeHint) is { } w) yield return w;
        if (AgentChromeHintDensityPolicy.From("sys", AgentSysChromeHint) is { } sy) yield return sy;
        if (AgentChromeHintDensityPolicy.From("onboard", AgentOnboardChromeHint) is { } o) yield return o;
        if (AgentChromeHintDensityPolicy.From("arch", AgentArchChromeHint) is { } a) yield return a;
        if (AgentChromeHintDensityPolicy.From("mcp", AgentMcpChromeHint) is { } m) yield return m;
        if (AgentChromeHintDensityPolicy.From("learn", AgentLearnChromeHint) is { } l) yield return l;
        if (AgentChromeHintDensityPolicy.From("domain", AgentDomainChromeHint) is { } d) yield return d;
        if (AgentChromeHintDensityPolicy.From("sa_desk", AgentSaDeskChromeHint) is { } sa) yield return sa;
    }

    void RaiseChromeHintDensity()
    {
        OnPropertyChanged(nameof(AgentChromeHintVisibleLines));
        OnPropertyChanged(nameof(AgentChromeHintOverflow));
        OnPropertyChanged(nameof(ShowAgentChromeHintOverflow));
    }

    partial void OnAgentCabinChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentPressureChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentIgniteChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentScopeChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentSysChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentOnboardChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentArchChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentMcpChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentPlanChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentReportChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentCrmChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentWebcamChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentToolchainChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentPluginsChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentRefactorChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentReviewChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentLearnChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentDomainChromeHintChanged(string? value) => RaiseChromeHintDensity();
    partial void OnAgentSaDeskChromeHintChanged(string? value) => RaiseChromeHintDensity();

    public void ApplyCabinOrganChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentCabinChromeHint, next, StringComparison.Ordinal)) return;
        AgentCabinChromeHint = next;
    }

    public void ApplyPressureChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentPressureChromeHint, next, StringComparison.Ordinal)) return;
        AgentPressureChromeHint = next;
    }

    public void ApplyIgniteChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentIgniteChromeHint, next, StringComparison.Ordinal)) return;
        AgentIgniteChromeHint = next;
    }

    public void ApplyScopeChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentScopeChromeHint, next, StringComparison.Ordinal)) return;
        AgentScopeChromeHint = next;
    }

    public void ApplySysChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentSysChromeHint, next, StringComparison.Ordinal)) return;
        AgentSysChromeHint = next;
    }

    public void ApplyOnboardChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentOnboardChromeHint, next, StringComparison.Ordinal)) return;
        AgentOnboardChromeHint = next;
    }

    public void ApplyArchChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentArchChromeHint, next, StringComparison.Ordinal)) return;
        AgentArchChromeHint = next;
    }

    public void ApplyMcpChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentMcpChromeHint, next, StringComparison.Ordinal)) return;
        AgentMcpChromeHint = next;
    }

    public void ApplyPlanChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentPlanChromeHint, next, StringComparison.Ordinal)) return;
        AgentPlanChromeHint = next;
    }

    public void ApplyReportChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentReportChromeHint, next, StringComparison.Ordinal)) return;
        AgentReportChromeHint = next;
    }

    public void ApplyCrmChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentCrmChromeHint, next, StringComparison.Ordinal)) return;
        AgentCrmChromeHint = next;
    }

    public void ApplyWebcamChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentWebcamChromeHint, next, StringComparison.Ordinal)) return;
        AgentWebcamChromeHint = next;
    }

    public void ApplyToolchainChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentToolchainChromeHint, next, StringComparison.Ordinal)) return;
        AgentToolchainChromeHint = next;
    }

    public void ApplyPluginsChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentPluginsChromeHint, next, StringComparison.Ordinal)) return;
        AgentPluginsChromeHint = next;
    }

    public void ApplyRefactorChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentRefactorChromeHint, next, StringComparison.Ordinal)) return;
        AgentRefactorChromeHint = next;
    }

    public void ApplyReviewChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentReviewChromeHint, next, StringComparison.Ordinal)) return;
        AgentReviewChromeHint = next;
    }

    public void ApplyLearnChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentLearnChromeHint, next, StringComparison.Ordinal)) return;
        AgentLearnChromeHint = next;
    }

    public void ApplyDomainChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentDomainChromeHint, next, StringComparison.Ordinal)) return;
        AgentDomainChromeHint = next;
    }

    public void ApplySaDeskChromeHint(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(AgentSaDeskChromeHint, next, StringComparison.Ordinal)) return;
        AgentSaDeskChromeHint = next;
    }
}

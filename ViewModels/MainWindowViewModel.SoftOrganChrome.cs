#nullable enable
using CascadeIDE.Features.UiChrome;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>SoftOrgan quiet-chrome band — VisibleLines + Overflow (Glass SoftOrganBand parity).</summary>
public partial class MainWindowViewModel
{
    bool _agentChromeHintsExpanded;

    public IReadOnlyList<string> AgentChromeHintVisibleLines => BuildChromeHintDensity().VisibleLines;
    public string? AgentChromeHintOverflow => BuildChromeHintDensity().OverflowLine;
    public bool ShowAgentChromeHintOverflow => !string.IsNullOrWhiteSpace(AgentChromeHintOverflow);

    AgentChromeHintDensityPolicy.Result BuildChromeHintDensity()
    {
        var r = AgentChromeHintDensityPolicy.Collapse(
            CollectChromeHintCandidates(),
            expanded: _agentChromeHintsExpanded);
        // Drop expand latch when density no longer needs overflow.
        if (!r.IsExpanded && _agentChromeHintsExpanded)
            _agentChromeHintsExpanded = false;
        return r;
    }

    [RelayCommand]
    void ToggleAgentChromeHintOverflow()
    {
        var candidates = CollectChromeHintCandidates().Count();
        var next = AgentChromeHintDensityPolicy.ToggleExpanded(
            _agentChromeHintsExpanded,
            candidates);
        if (next == _agentChromeHintsExpanded)
            return;
        _agentChromeHintsExpanded = next;
        RaiseChromeHintDensity();
    }

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
}

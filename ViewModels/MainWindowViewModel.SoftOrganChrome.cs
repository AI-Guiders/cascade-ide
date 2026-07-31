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
        (string Id, string? Text)[] seats =
        [
            ("pressure", AgentPressureChromeHint),
            ("ignite", AgentIgniteChromeHint),
            ("plan", AgentPlanChromeHint),
            ("cabin", AgentCabinChromeHint),
            ("scope", AgentScopeChromeHint),
            ("review", AgentReviewChromeHint),
            ("refactor", AgentRefactorChromeHint),
            ("plugins", AgentPluginsChromeHint),
            ("toolchain", AgentToolchainChromeHint),
            ("crm", AgentCrmChromeHint),
            ("report", AgentReportChromeHint),
            ("webcam", AgentWebcamChromeHint),
            ("sys", AgentSysChromeHint),
            ("onboard", AgentOnboardChromeHint),
            ("arch", AgentArchChromeHint),
            ("mcp", AgentMcpChromeHint),
            ("learn", AgentLearnChromeHint),
            ("domain", AgentDomainChromeHint),
            ("sa_desk", AgentSaDeskChromeHint),
        ];

        foreach (var (id, text) in seats)
        {
            if (AgentChromeHintDensityPolicy.From(id, text) is { } h)
                yield return h;
        }
    }


    void RaiseChromeHintDensity()
    {
        OnPropertyChanged(nameof(AgentChromeHintVisibleLines));
        OnPropertyChanged(nameof(AgentChromeHintOverflow));
        OnPropertyChanged(nameof(ShowAgentChromeHintOverflow));
    }

}

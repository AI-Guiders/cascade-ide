#nullable enable
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>SoftOrgan quiet-chrome — Show*/Apply*/VisibleLines+Overflow (Glass SoftOrganBand parity).</summary>
public partial class MainWindowViewModel
{
    bool _agentChromeHintsExpanded;

    public IReadOnlyList<string> AgentChromeHintVisibleLines => BuildChromeHintDensity().VisibleLines;
    public string? AgentChromeHintOverflow => BuildChromeHintDensity().OverflowLine;
    public bool ShowAgentChromeHintOverflow => !string.IsNullOrWhiteSpace(AgentChromeHintOverflow);

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

    /// <summary>Область разметки над нижним доком: Workspace Health и/или полоса EICAS (<see cref="Views.WorkspaceChromeBandView"/>).</summary>
    public bool ShowWorkspaceChromeBand =>
        MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            ShowIdeHealthStrip,
            ShowEicasAlertsBar,
            ShowAgentCabinChromeHint,
            ShowAgentPressureChromeHint,
            ShowAgentIgniteChromeHint,
            ShowAgentScopeChromeHint,
            ShowAgentSysChromeHint,
            ShowAgentOnboardChromeHint,
            ShowAgentArchChromeHint,
            ShowAgentMcpChromeHint,
            ShowAgentPlanChromeHint,
            ShowAgentReportChromeHint,
            ShowAgentCrmChromeHint,
            ShowAgentWebcamChromeHint,
            ShowAgentToolchainChromeHint,
            ShowAgentPluginsChromeHint,
            ShowAgentRefactorChromeHint,
            ShowAgentReviewChromeHint,
            ShowAgentLearnChromeHint,
            ShowAgentDomainChromeHint,
            ShowAgentSaDeskChromeHint);

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

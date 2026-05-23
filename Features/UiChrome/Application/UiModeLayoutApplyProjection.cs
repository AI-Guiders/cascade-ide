using CascadeIDE.Contracts;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.Features.UiChrome.Application;

/// <summary>План применения раскладки режима UI (без присваивания observable VM).</summary>
[PresentationProjection("ui mode layout apply plan")]
public static class UiModeLayoutApplyProjection
{
    public sealed record Plan(UiModeLayoutSpec Spec, string NormalizedMode, bool Persist);

    public static Plan Create(string? mode, bool persist)
    {
        var normalized = UiChromeViewModel.NormalizeUiMode(mode);
        return new Plan(UiModeCatalog.GetSpec(normalized), normalized, persist);
    }

    public static MfdShellPage ResolveMfdPageAfterApply(Plan plan, MfdShellPage currentPage) =>
        plan.Spec.SelectTerminalTabWhenTerminalShown && plan.Spec.TerminalVisible
            ? MfdShellPage.Terminal
            : currentPage;

    public static void PersistWorkspaceMode(
        Plan plan,
        bool pfdRegionExpanded,
        bool terminalVisible,
        CascadeIdeSettings settings,
        Action saveSettingsIfChanged)
    {
        if (!plan.Persist)
            return;

        settings.Workspace.Mode = plan.NormalizedMode;
        settings.Workspace.PfdExpanded = pfdRegionExpanded;
        settings.Workspace.ShowTerminal = terminalVisible;
        saveSettingsIfChanged();
    }
}

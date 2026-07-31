using Avalonia.Threading;
using CascadeIDE.Features.Shell;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Shell chrome change handlers: UI mode, region expand, panel visibility, MFD page.</summary>
public partial class MainWindowViewModel
{
    internal void HandleShellUiModeChanged(string value)
    {
        var normalized = NormalizeUiMode(value);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UiMode = normalized;
            return;
        }

        ApplyUiModeLayout(normalized, persist: true);
        Autonomous.NotifyHostPowerContextChanged();
        if (string.Equals(normalized, "Power", StringComparison.OrdinalIgnoreCase))
            UiScheduler.Default.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Chrome.NotifyUiModeChangedForBloom(normalized);
        RefreshCommandPaletteIfOpen();
    }

    internal void HandleShellIsPfdRegionExpandedChanged(bool value)
    {
        _settings.Workspace.PfdExpanded = value;
        SaveSettingsIfChanged();
        if (value)
            ScheduleWorkspaceNavigationMapRefresh();
    }

    internal void HandleShellIsTerminalVisibleChanged(bool value)
    {
        _settings.Workspace.ShowTerminal = value;
        SaveSettingsIfChanged();
        if (value)
            TryNavigateToMfdShellPage(MfdShellPage.Terminal);
        else if (ShellSettingsPresentationProjection.ShouldCoerceCurrentPageWhenHidden(
                     CurrentMfdShellPage,
                     MfdShellPage.Terminal))
            CoerceMfdShellPageToAllowed();
    }

    internal void HandleShellIsBuildOutputVisibleChanged(bool value)
    {
        if (value)
            TryNavigateToMfdShellPage(MfdShellPage.Build);
        else if (ShellSettingsPresentationProjection.ShouldCoerceCurrentPageWhenHidden(
                     CurrentMfdShellPage,
                     MfdShellPage.Build))
            CoerceMfdShellPageToAllowed();
    }

    internal void HandleShellIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.Workspace.ShowInstrumentation = value;
        SaveSettingsIfChanged();
        if (value)
        {
            TryNavigateToMfdShellPage(MfdShellPage.Events);
            return;
        }

        if (ShellSettingsPresentationProjection.ShouldCoerceWhenInstrumentationHidden(CurrentMfdShellPage))
            CoerceMfdShellPageToAllowed();
    }

    internal void HandleShellIsMfdRegionExpandedChanged(bool value)
    {
        // Intent «развёрнут/свёрнут регион Mfd» в раскладке (ширина в MainGrid через композитор).
    }

    internal void HandleShellIsGitPanelVisibleChanged(bool value)
    {
        _settings.Workspace.ShowGit = value;
        SaveSettingsIfChanged();
        if (value)
        {
            TryNavigateToMfdShellPage(MfdShellPage.Git);
            _ = GitPanel.RefreshGitPanelAsync();
        }
        else if (ShellSettingsPresentationProjection.ShouldCoerceCurrentPageWhenHidden(
                     CurrentMfdShellPage,
                     MfdShellPage.Git))
            CoerceMfdShellPageToAllowed();
    }

    internal void HandleShellCurrentMfdShellPageChanged(MfdShellPage value)
    {
        if (!IsMfdShellPageAllowed(value))
        {
            CoerceMfdShellPageToAllowed();
            return;
        }

        if (value == MfdShellPage.EnvironmentReadiness)
            _ = RefreshEnvironmentReadinessAsync();

        if (value == MfdShellPage.HybridIndex)
        {
            EnsureHybridIndexSubscription();
            RaiseHybridIndexPresentationProperties();
        }

        if (value == MfdShellPage.RelatedFiles)
            ScheduleWorkspaceNavigationMapRefresh();

        if (value == MfdShellPage.Correspondence)
            ScheduleWorkspaceNavigationMapRefresh();
    }
}

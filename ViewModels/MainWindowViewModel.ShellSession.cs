using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Threading;
using CascadeIDE.Features.Shell;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Services;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Wave 2 этап 3: <see cref="ShellChromeViewModel"/> + прокси на MWVM для привязок и presentation.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>Регионы MainGrid, MFD-страница, режим UI, флаги панелей.</summary>
    public ShellChromeViewModel Shell { get; private set; } = null!;

    public ObservableCollection<FocusPlanItemViewModel> FocusPlanItems { get; } = [];

    public bool IsMfdRegionExpanded
    {
        get => Shell.IsMfdRegionExpanded;
        set => Shell.IsMfdRegionExpanded = value;
    }

    public bool IsPfdRegionExpanded
    {
        get => Shell.IsPfdRegionExpanded;
        set => Shell.IsPfdRegionExpanded = value;
    }

    public bool IsTerminalVisible
    {
        get => Shell.IsTerminalVisible;
        set => Shell.IsTerminalVisible = value;
    }

    public bool IsGitPanelVisible
    {
        get => Shell.IsGitPanelVisible;
        set => Shell.IsGitPanelVisible = value;
    }

    public bool IsBuildOutputVisible
    {
        get => Shell.IsBuildOutputVisible;
        set => Shell.IsBuildOutputVisible = value;
    }

    public bool IsInstrumentationDockVisible
    {
        get => Shell.IsInstrumentationDockVisible;
        set => Shell.IsInstrumentationDockVisible = value;
    }

    public MfdShellPage CurrentMfdShellPage
    {
        get => Shell.CurrentMfdShellPage;
        set => Shell.CurrentMfdShellPage = value;
    }

    public CommandPaletteHost CommandPaletteHost
    {
        get => Shell.CommandPaletteHost;
        set => Shell.CommandPaletteHost = value;
    }

    public string UiMode
    {
        get => Shell.UiMode;
        set => Shell.UiMode = value;
    }

    public int EditorGroupCount
    {
        get => Shell.EditorGroupCount;
        set => Shell.EditorGroupCount = value;
    }

    public string WorkspaceSnapshotJson
    {
        get => Shell.WorkspaceSnapshotJson;
        set => Shell.WorkspaceSnapshotJson = value;
    }

    public bool IsBuilding
    {
        get => Shell.IsBuilding;
        set => Shell.IsBuilding = value;
    }

    private void OnShellChromePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
            return;

        OnPropertyChanged(e.PropertyName);

        foreach (var dependent in ShellChromePresentationRelay.GetDependents(e.PropertyName))
            OnPropertyChanged(dependent);

        switch (e.PropertyName)
        {
            case nameof(ShellChromeViewModel.IsMfdRegionExpanded):
                Shell.ToggleMfdRegionExpandedCommand.NotifyCanExecuteChanged();
                break;
            case nameof(ShellChromeViewModel.IsPfdRegionExpanded):
                Shell.TogglePfdRegionExpandedCommand.NotifyCanExecuteChanged();
                break;
            case nameof(ShellChromeViewModel.IsBuilding):
                BuildSolutionCommand.NotifyCanExecuteChanged();
                break;
        }
    }

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
    }
}

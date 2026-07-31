using System.Collections.ObjectModel;
using System.ComponentModel;
using CascadeIDE.Features.Shell;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Wave 2 этап 3: <see cref="ShellChromeViewModel"/> + прокси на MWVM для привязок и presentation.
/// Handlers → <c>ShellSession.Handlers</c>.
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
            case nameof(ShellChromeViewModel.CommandPaletteHost):
                CockpitCommandLineOverlay?.NotifyShellPresentationChanged();
                break;
        }
    }
}

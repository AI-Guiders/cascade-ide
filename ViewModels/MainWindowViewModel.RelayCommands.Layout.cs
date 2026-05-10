using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Relay: регионы, панели MFD, группы редакторов.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void TogglePfdRegionExpanded() =>
        ApplyPfdRegionExpanded(!IsPfdRegionExpanded);

    [RelayCommand]
    private void ToggleBuildOutput()
    {
        IsBuildOutputVisible = !IsBuildOutputVisible;
        if (IsBuildOutputVisible)
            TryNavigateToMfdShellPage(MfdShellPage.Build);
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
        if (IsTerminalVisible)
            TryNavigateToMfdShellPage(MfdShellPage.Terminal);
    }

    [RelayCommand]
    private void ToggleInstrumentationDock() => IsInstrumentationDockVisible = !IsInstrumentationDockVisible;

    [RelayCommand]
    private void SetSingleEditorGroup() => EditorGroupCount = 1;

    [RelayCommand]
    private void SetDualEditorGroup() => EditorGroupCount = 2;

    [RelayCommand]
    private void SetTripleEditorGroup() => EditorGroupCount = 3;

    [RelayCommand]
    private void ShowPfdRegionPanel() => ApplyPfdRegionExpanded(true);

    [RelayCommand]
    private void ShowBuildOutputPanel()
    {
        IsBuildOutputVisible = true;
        TryNavigateToMfdShellPage(MfdShellPage.Build);
    }

    [RelayCommand]
    private void ShowChatPage()
    {
        ApplyMfdRegionExpanded(true);
        TryNavigateToMfdShellPage(MfdShellPage.Chat);
    }

    [RelayCommand]
    private void ShowSolutionExplorerPage()
    {
        ApplyMfdRegionExpanded(true);
        TryNavigateToMfdShellPage(MfdShellPage.SolutionExplorer);
    }

    [RelayCommand]
    private void ShowRelatedFilesMfdPage()
    {
        ApplyMfdRegionExpanded(true);
        TryNavigateToMfdShellPage(MfdShellPage.RelatedFiles);
    }

    [RelayCommand]
    private void ShowTerminalPanel()
    {
        IsTerminalVisible = true;
        TryNavigateToMfdShellPage(MfdShellPage.Terminal);
    }
}

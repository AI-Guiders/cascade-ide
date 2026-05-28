using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Shell;

/// <summary>Relay: регионы, панели MFD, группы редакторов.</summary>
public sealed partial class ShellChromeViewModel
{
    [RelayCommand]
    private void TogglePfdRegionExpanded() =>
        _host.ApplyPfdRegionExpanded(!IsPfdRegionExpanded);

    [RelayCommand]
    private void ToggleMfdRegionExpanded() =>
        _host.ApplyMfdRegionExpanded(!IsMfdRegionExpanded);

    [RelayCommand]
    private void ToggleBuildOutput()
    {
        IsBuildOutputVisible = !IsBuildOutputVisible;
        if (IsBuildOutputVisible)
            _host.TryNavigateToMfdShellPage(MfdShellPage.Build);
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
        if (IsTerminalVisible)
            _host.TryNavigateToMfdShellPage(MfdShellPage.Terminal);
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
    private void ShowPfdRegionPanel() => _host.ApplyPfdRegionExpanded(true);

    [RelayCommand]
    private void ShowBuildOutputPanel()
    {
        IsBuildOutputVisible = true;
        _host.TryNavigateToMfdShellPage(MfdShellPage.Build);
    }

    [RelayCommand]
    private void ShowChatPage()
    {
        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.Chat);
    }

    [RelayCommand]
    private void ShowSolutionExplorerPage()
    {
        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.SolutionExplorer);
    }

    [RelayCommand]
    private void ShowRelatedFilesMfdPage()
    {
        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.RelatedFiles);
    }

    [RelayCommand]
    private void ShowCorrespondencePage()
    {
        if (_host.NavigationMap.ShowCorrespondencePageCommand.CanExecute(null))
            _host.NavigationMap.ShowCorrespondencePageCommand.Execute(null);
    }

    [RelayCommand]
    private void ShowTerminalPanel()
    {
        IsTerminalVisible = true;
        _host.TryNavigateToMfdShellPage(MfdShellPage.Terminal);
    }
}

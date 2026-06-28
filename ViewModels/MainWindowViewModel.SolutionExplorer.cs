using System.Collections.ObjectModel;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Solution Explorer: фильтр, compact, track active, индекс файлов (ADR 0167).</summary>
public partial class MainWindowViewModel
{
    private readonly WorkspaceFileIndex _workspaceFileIndex = new();

    public WorkspaceFileIndex WorkspaceFileIndex => _workspaceFileIndex;

    [ObservableProperty]
    private bool _solutionExplorerTrackActiveItem;

    [ObservableProperty]
    private bool _solutionExplorerCompactTree;

    [ObservableProperty]
    private string _solutionExplorerFilterText = "";

    [ObservableProperty]
    private ObservableCollection<SolutionItem> _solutionExplorerDisplayRoots = [];

    [ObservableProperty]
    private SolutionItem? _solutionExplorerSelectedItem;

    internal void ApplySolutionExplorerSettingsFromModel(SolutionExplorerSettings settings)
    {
        SolutionExplorerTrackActiveItem = settings.TrackActiveItem;
        SolutionExplorerCompactTree = settings.CompactTree;
    }

    partial void OnSolutionExplorerFilterTextChanged(string value) =>
        RefreshSolutionExplorerTreeFilter();

    partial void OnSolutionExplorerTrackActiveItemChanged(bool value)
    {
        _settings.Workspace.SolutionExplorer.TrackActiveItem = value;
        SaveSettingsIfChanged();
        if (value)
            Documents.SyncSelectedSolutionItemToCurrentFile();
    }

    partial void OnSolutionExplorerCompactTreeChanged(bool value)
    {
        _settings.Workspace.SolutionExplorer.CompactTree = value;
        SaveSettingsIfChanged();
    }

    internal void InvalidateWorkspaceFileIndex() =>
        _workspaceFileIndex.Invalidate(
            Workspace.SolutionRoots,
            Workspace.SolutionPath,
            GetWorkspacePath() ?? "");

    internal void RefreshSolutionExplorerTreeFilter()
    {
        InvalidateWorkspaceFileIndex();
        SolutionExplorerTreeFilter.RebuildDisplayRoots(
            Workspace.SolutionRoots,
            SolutionExplorerDisplayRoots,
            SolutionExplorerFilterText,
            _workspaceFileIndex);
    }

    partial void OnSolutionExplorerSelectedItemChanged(SolutionItem? value)
    {
        if (value?.FullPath is { } path
            && SolutionTreePath.TryGetFullPath(path, out var normalized))
        {
            Workspace.SelectedSolutionItem =
                SolutionTreePath.FindItemByFullPath(Workspace.SolutionRoots, normalized) ?? value;
            return;
        }

        Workspace.SelectedSolutionItem = value;
    }

    [RelayCommand]
    private void OpenGoToFilePalette()
    {
        IsCommandPaletteOpen = true;
        CommandPaletteQuery = "f:";
        RefreshCommandPaletteFilter();
        CommandPaletteSelectedIndex = CommandPaletteSelectionProjection.InitialSelectedIndex(
            FilteredCommandPaletteEntries.Count);
    }

    [RelayCommand]
    public void FocusSolutionExplorerFilter()
    {
        Shell.ShowSolutionExplorerPageCommand.Execute(null);
        SolutionExplorerFilterFocusRequested?.Invoke();
    }

    /// <summary>View подписывается, чтобы сфокусировать поле фильтра SE.</summary>
    internal event Action? SolutionExplorerFilterFocusRequested;

    [RelayCommand]
    private void OpenSelectedSolutionItem()
    {
        var item = Workspace.SelectedSolutionItem;
        if (item?.FullPath is not { } path || Directory.Exists(path))
            return;
        Documents.OpenOrActivateDocument(path);
    }

    [RelayCommand]
    private async Task CopySelectedSolutionItemPathAsync()
    {
        var item = Workspace.SelectedSolutionItem;
        if (item?.FullPath is not { } path)
            return;
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(path);
    }

    [RelayCommand]
    private void RevealSelectedSolutionItemInExplorer()
    {
        var item = Workspace.SelectedSolutionItem;
        if (item?.FullPath is not { } path)
            return;
        WindowsShellReveal.TryRevealInExplorer(path);
    }
}

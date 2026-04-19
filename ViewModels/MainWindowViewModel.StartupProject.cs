using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Стартовый проект.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    private string? _startupProjectCsprojFullPath;

    [ObservableProperty]
    private string _startupProjectShortLabel = "";

    /// <summary>Краткая подпись для панели дерева: имя стартового проекта для F5.</summary>
    public bool HasStartupProject => !string.IsNullOrEmpty(StartupProjectCsprojFullPath);

    public string StartupProjectBanner =>
        HasStartupProject ? $"Старт отладки (F5): {StartupProjectShortLabel}" : "";

    partial void OnStartupProjectCsprojFullPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasStartupProject));
        OnPropertyChanged(nameof(StartupProjectBanner));
        SetStartupProjectFromSelectionCommand.NotifyCanExecuteChanged();
        ClearStartupProjectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>После загрузки дерева: прочитать <c>.cascade-ide/startup-project.json</c> и проверить, что путь есть в решении.</summary>
    public void RefreshStartupProjectAfterSolutionLoad()
    {
        StartupProjectCsprojFullPath = null;
        StartupProjectShortLabel = "";

        var sln = Workspace.SolutionPath;
        if (string.IsNullOrEmpty(sln) || Workspace.SolutionRoots.Count == 0)
            return;

        if (!StartupProjectStore.TryLoad(sln, out var rel) || string.IsNullOrEmpty(rel))
            return;

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return;

        var full = Path.GetFullPath(Path.Combine(solutionDir, rel));
        if (!File.Exists(full))
        {
            StartupProjectStore.Clear(sln);
            return;
        }

        var projects = McpSolutionTree.CollectProjectPaths(Workspace.SolutionRoots)
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!projects.Contains(full))
        {
            StartupProjectStore.Clear(sln);
            return;
        }

        ApplyStartupProject(full);
    }

    private void ApplyStartupProject(string csprojFullPath)
    {
        StartupProjectCsprojFullPath = csprojFullPath;
        StartupProjectShortLabel = Path.GetFileNameWithoutExtension(csprojFullPath);
    }

    private void ClearStartupProjectInMemoryOnly()
    {
        StartupProjectCsprojFullPath = null;
        StartupProjectShortLabel = "";
    }

    [RelayCommand(CanExecute = nameof(CanSetStartupProjectFromSelection))]
    private async Task SetStartupProjectFromSelectionAsync()
    {
        var item = Workspace.SelectedSolutionItem;
        var path = item?.FullPath;
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return;

        var sln = Workspace.SolutionPath;
        if (string.IsNullOrEmpty(sln))
            return;

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return;

        var full = Path.GetFullPath(path);
        try
        {
            var rel = Path.GetRelativePath(solutionDir, full);
            if (rel.StartsWith("..", StringComparison.Ordinal))
            {
                await ShowDebugInfoAsync("Стартовый проект",
                        "Проект должен находиться внутри каталога решения.")
                    .ConfigureAwait(false);
                return;
            }

            StartupProjectStore.Save(sln, rel);
            ApplyStartupProject(full);
        }
        catch (Exception ex)
        {
            await ShowDebugInfoAsync("Стартовый проект", ex.Message).ConfigureAwait(false);
        }
    }

    private bool CanSetStartupProjectFromSelection()
    {
        var p = Workspace.SelectedSolutionItem?.FullPath;
        return !string.IsNullOrEmpty(p) && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand(CanExecute = nameof(CanClearStartupProject))]
    private void ClearStartupProject()
    {
        var sln = Workspace.SolutionPath;
        if (!string.IsNullOrEmpty(sln))
            StartupProjectStore.Clear(sln);
        ClearStartupProjectInMemoryOnly();
    }

    private bool CanClearStartupProject() => HasStartupProject;

    private async Task<string?> TryResolveStartupDebugTargetAsync()
    {
        var csproj = StartupProjectCsprojFullPath;
        if (string.IsNullOrEmpty(csproj) || !File.Exists(csproj))
            return null;

        var (target, err) = await MsBuildDebugTargetResolver.TryResolveAsync(csproj, _dotnetRunner).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(target))
            return target;

        if (!string.IsNullOrEmpty(err))
            await ShowDebugInfoAsync("Стартовый проект", err).ConfigureAwait(false);
        return null;
    }
}

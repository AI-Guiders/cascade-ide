using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Launch.Application;
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
        StartupProjectBannerProjection.Format(
            HasStartupProject,
            ShowLaunchProfilePicker,
            SelectedLaunchProfileId,
            StartupProjectShortLabel);

    partial void OnStartupProjectCsprojFullPathChanged(string? value)
    {
        PublishToIdeDataBusAndRebuild(new StartupProjectPathChanged(value));
        OnPropertyChanged(nameof(HasStartupProject));
        OnPropertyChanged(nameof(StartupProjectBanner));
        SetStartupProjectFromSelectionCommand.NotifyCanExecuteChanged();
        ClearStartupProjectCommand.NotifyCanExecuteChanged();
    }

    partial void OnStartupProjectShortLabelChanged(string value) =>
        OnPropertyChanged(nameof(StartupProjectBanner));

    /// <summary>После загрузки дерева: прочитать <c>launch-profiles.toml</c> (миграция с <c>startup-project.json</c>) и проверить, что путь есть в решении.</summary>
    public void RefreshStartupProjectAfterSolutionLoad()
    {
        StartupProjectCsprojFullPath = null;
        StartupProjectShortLabel = "";

        var sln = Workspace.SolutionPath;
        var solutionDir = string.IsNullOrEmpty(sln)
            ? null
            : Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
        {
            RefreshLaunchProfilePickerFromStore();
            return;
        }

        var resolved = StartupProjectRefreshProjection.ResolveAfterSolutionLoad(
            sln,
            Workspace.SolutionRoots,
            solutionDir);
        if (!string.IsNullOrEmpty(resolved.CsprojFullPath))
            ApplyStartupProject(resolved.CsprojFullPath);

        RefreshLaunchProfilePickerFromStore();
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

        var full = CanonicalFilePath.Normalize(path);
        try
        {
            if (!LaunchProjectRelativePath.TryGetRelativeToSolutionDirectory(solutionDir, full, out var rel, out var relErr))
            {
                await ShowDebugInfoAsync("Стартовый проект",
                        string.IsNullOrEmpty(relErr) ? "Не удалось вычислить относительный путь к проекту." : relErr)
                    .ConfigureAwait(false);
                return;
            }

            StartupProjectStore.Save(sln, rel);
            ApplyStartupProject(full);
            RefreshLaunchProfilePickerFromStore();
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

    /// <summary>MSBuild + launch profile (ADR 0090) или унаследованный стартовый <c>.csproj</c>.</summary>
    private Task<DebugLaunchResolution?> TryResolveDebugLaunchForF5Async() =>
        DebugLaunchForF5Orchestrator.TryResolveAsync(
            Workspace.SolutionRoots,
            CurrentFilePath,
            Workspace.SelectedSolutionItem?.FullPath,
            () => StartupProjectCsprojFullPath,
            ApplyStartupProject,
            Workspace.SolutionPath,
            Services.BreakpointsFileService.GetWorkspaceRoot(Workspace.SolutionPath),
            _dotnetRunner,
            ShowDebugInfoAsync);

    /// <summary>
    /// Режим B <c>debug_launch</c> (MCP): <paramref name="profileName"/> или активный профиль; при явном <paramref name="mcpProgramArgs"/> — вместо аргументов из профиля.
    /// </summary>
    internal Task<string> DebugLaunchByProfileOrResolvedTargetAsync(
        string workspacePath,
        string? targetPath,
        string? profileName,
        string? netcoredbgPath,
        IReadOnlyList<string>? mcpProgramArgs,
        CancellationToken cancellationToken = default) =>
        DebugLaunchByProfileMcpOrchestrator.RunAsync(
            workspacePath,
            targetPath,
            profileName,
            netcoredbgPath,
            mcpProgramArgs,
            _dotnetRunner,
            DapDebug,
            cancellationToken);

}

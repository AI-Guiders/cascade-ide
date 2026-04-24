using System.Collections.ObjectModel;
using CascadeIDE.Models;
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
        HasStartupProject
            ? (ShowLaunchProfilePicker && !string.IsNullOrEmpty(SelectedLaunchProfileId)
                ? $"Старт отладки (F5): {StartupProjectShortLabel} · {SelectedLaunchProfileId}"
                : $"Старт отладки (F5): {StartupProjectShortLabel}")
            : "";

    partial void OnStartupProjectCsprojFullPathChanged(string? value)
    {
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
        if (string.IsNullOrEmpty(sln) || Workspace.SolutionRoots.Count == 0)
        {
            RefreshLaunchProfilePickerFromStore();
            return;
        }

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return;

        var projects = McpSolutionTree.CollectProjectPaths(Workspace.SolutionRoots)
            .Select(static p => Path.GetFullPath(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fromStore = false;
        if (StartupProjectStore.TryLoad(sln, out var rel) && !string.IsNullOrEmpty(rel))
        {
            var full = Path.GetFullPath(Path.Combine(solutionDir, rel));
            if (File.Exists(full) && projects.Contains(full))
            {
                ApplyStartupProject(full);
                fromStore = true;
            }
            else
                StartupProjectStore.Clear(sln);
        }

        if (!fromStore)
            TryApplyDefaultSingleProjectStartup(sln, solutionDir, projects);

        RefreshLaunchProfilePickerFromStore();
    }

    /// <summary>Единственный <c>.csproj</c> в дереве — считаем его стартовым и сохраняем, чтобы F5 не открывал диалог выбора DLL.</summary>
    private void TryApplyDefaultSingleProjectStartup(string sln, string solutionDir, HashSet<string> projectPathSet)
    {
        var csprojs = CollectDistinctProjectFilePaths(Workspace.SolutionRoots);
        if (csprojs.Count != 1)
            return;

        var only = csprojs[0];
        if (!projectPathSet.Contains(only))
            return;

        try
        {
            var rel = Path.GetRelativePath(solutionDir, only);
            if (rel.StartsWith("..", StringComparison.Ordinal))
                return;
            StartupProjectStore.Save(sln, rel);
        }
        catch
        {
            // сохранение опционально — в памяти стартовый проект всё равно будет
        }

        ApplyStartupProject(only);
    }

    private static List<string> CollectDistinctProjectFilePaths(ObservableCollection<SolutionItem> roots)
    {
        return McpSolutionTree.CollectProjectPaths(roots)
            .Where(static p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                              p.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
            .Select(static p => Path.GetFullPath(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Перед MSBuild: если стартовый не задан или битой ссылкой, выбрать единственный проект, проект по активному файлу
    /// или выбранный в обозревателе <c>.csproj</c> (как ожидается от F5 / «текущий код»).
    /// </summary>
    private void TryApplyInferredStartupProjectForDebug()
    {
        if (!string.IsNullOrEmpty(StartupProjectCsprojFullPath) && File.Exists(StartupProjectCsprojFullPath))
            return;

        if (Workspace.SolutionRoots.Count == 0)
            return;

        var csprojs = CollectDistinctProjectFilePaths(Workspace.SolutionRoots);
        var set = csprojs.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (csprojs.Count == 1)
        {
            ApplyStartupProject(csprojs[0]);
            return;
        }

        var fp = CurrentFilePath;
        if (!string.IsNullOrEmpty(fp))
        {
            try
            {
                var full = Path.GetFullPath(fp);
                if (File.Exists(full) && !McpSolutionTree.IsBuildArtifactPath(full))
                {
                    if (McpSolutionTree.MapFileToProject(Workspace.SolutionRoots).TryGetValue(full, out var treeProj) &&
                        !string.IsNullOrEmpty(treeProj) && set.Contains(treeProj))
                    {
                        ApplyStartupProject(treeProj);
                        return;
                    }

                    var disk = McpSolutionTree.ResolveOwningProjectPath(full);
                    if (!string.IsNullOrEmpty(disk) && set.Contains(disk))
                    {
                        ApplyStartupProject(disk);
                        return;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        var sel = Workspace.SelectedSolutionItem?.FullPath;
        if (!string.IsNullOrEmpty(sel) &&
            (sel.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
             sel.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)) &&
            File.Exists(sel))
        {
            var p = Path.GetFullPath(sel);
            if (set.Contains(p))
                ApplyStartupProject(p);
        }
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
    private async Task<DebugLaunchResolution?> TryResolveDebugLaunchForF5Async()
    {
        TryApplyInferredStartupProjectForDebug();
        var sln = Workspace.SolutionPath;
        if (string.IsNullOrEmpty(sln))
            return null;

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return null;

        if (LaunchProfilesStore.TryResolveProfileForLaunch(sln, profileName: null, out var prof, out _) &&
            !string.IsNullOrWhiteSpace(prof.ProjectRelativeToSolution) &&
            DebugLaunchFromProfile.TryGetExistingCsprojFullPath(solutionDir, prof.ProjectRelativeToSolution, out var csprojFull))
        {
            var (target, err) = await MsBuildDebugTargetResolver
                .TryResolveAsync(csprojFull, _dotnetRunner, prof.Configuration)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(target))
                return DebugLaunchFromProfile.ToResolution(prof, target);

            if (!string.IsNullOrEmpty(err))
                await ShowDebugInfoAsync("Стартовый проект", err).ConfigureAwait(false);
            return null;
        }

        var csproj = StartupProjectCsprojFullPath;
        if (string.IsNullOrEmpty(csproj) || !File.Exists(csproj))
            return null;

        var (t2, e2) = await MsBuildDebugTargetResolver
            .TryResolveAsync(csproj, _dotnetRunner, LaunchProfilesStore.DefaultConfiguration)
            .ConfigureAwait(false);
        if (!string.IsNullOrEmpty(t2))
            return new DebugLaunchResolution(t2, null, null, null, OpenLaunchBrowser: false, LaunchUrl: null);

        if (!string.IsNullOrEmpty(e2))
            await ShowDebugInfoAsync("Стартовый проект", e2).ConfigureAwait(false);
        return null;
    }

    /// <summary>
    /// Режим B <c>debug_launch</c> (MCP): <paramref name="profileName"/> или активный профиль; при явном <paramref name="mcpProgramArgs"/> — вместо аргументов из профиля.
    /// </summary>
    internal async Task<string> DebugLaunchByProfileOrResolvedTargetAsync(
        string workspacePath,
        string? targetPath,
        string? profileName,
        string? netcoredbgPath,
        IReadOnlyList<string>? mcpProgramArgs,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(targetPath))
        {
            return await DapDebug.LaunchAsync(
                workspacePath,
                targetPath!,
                netcoredbgPath,
                mcpProgramArgs,
                environment: null,
                workingDirectoryOverride: null,
                cancellationToken).ConfigureAwait(false);
        }

        var sln = DebugWorkspacePath.TryResolveWorkspaceToSolutionPath(workspacePath);
        if (string.IsNullOrEmpty(sln))
            return "# Error: no_solution_in_workspace: укажи каталог с .sln или путь к .sln, либо target_path к .dll.";

        if (!LaunchProfilesStore.TryResolveProfileForLaunch(sln, profileName, out var prof, out var perr))
            return "# Error: " + perr;

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return "# Error: workspace_root_unresolved.";

        if (!DebugLaunchFromProfile.TryGetExistingCsprojFullPath(solutionDir, prof.ProjectRelativeToSolution, out var csprojFull))
        {
            var candidate = Path.GetFullPath(Path.Combine(solutionDir, prof.ProjectRelativeToSolution));
            return "# Error: project_not_found: " + candidate;
        }

        var (target, err) = await MsBuildDebugTargetResolver
            .TryResolveAsync(csprojFull, _dotnetRunner, prof.Configuration, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrEmpty(target))
            return "# Error: " + (err ?? "msbuild_unresolved");

        var prg = mcpProgramArgs is { Count: > 0 } ? mcpProgramArgs : prof.ProgramArgs;
        IReadOnlyDictionary<string, string>? env = DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(prof);
        var launchResult = await DapDebug.LaunchAsync(
            workspacePath,
            target,
            netcoredbgPath,
            prg,
            env,
            prof.WorkingDirectoryRelative,
            cancellationToken).ConfigureAwait(false);
        if (prof.OpenLaunchBrowser)
            KestrelLaunchBrowser.TryOpenAfterLaunch(
                DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(prof),
                prof.LaunchUrl);
        return launchResult;
    }
}

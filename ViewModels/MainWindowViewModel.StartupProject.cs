using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.DataBus;
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
        if (string.IsNullOrEmpty(sln) || Workspace.SolutionRoots.Count == 0)
        {
            RefreshLaunchProfilePickerFromStore();
            return;
        }

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return;

        var projects = McpSolutionTree.CollectProjectPaths(Workspace.SolutionRoots)
            .Select(static p => CanonicalFilePath.Normalize(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fromStore = false;
        if (StartupProjectStore.TryLoad(sln, out var rel) && !string.IsNullOrEmpty(rel))
        {
            var full = CanonicalFilePath.Normalize(Path.Combine(solutionDir, rel));
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
            .Select(static p => CanonicalFilePath.Normalize(p))
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
                var full = CanonicalFilePath.Normalize(fp);
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
            var p = CanonicalFilePath.Normalize(sel);
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

        var full = CanonicalFilePath.Normalize(path);
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
        var hasSolution = !string.IsNullOrEmpty(sln);
        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        var hasWorkspaceRoot = !string.IsNullOrEmpty(solutionDir);

        var csproj = StartupProjectCsprojFullPath;
        var startupCsprojFull = !string.IsNullOrEmpty(csproj) && File.Exists(csproj) ? csproj : null;
        var preResolve = hasSolution && hasWorkspaceRoot
            ? LaunchPreResolvePipelineUnit.Default.Compose(
                sln!,
                explicitProfileName: null,
                solutionDirectory: solutionDir!,
                startupProjectFullPath: startupCsprojFull)
            : new LaunchPreResolvePipelineSnapshot(
                Profile: null,
                ProfileProjectCsprojFullPath: null,
                Readiness: LaunchReadinessUnit.Default.Compose(
                    hasSolutionPath: hasSolution,
                    hasWorkspaceRoot: hasWorkspaceRoot,
                    profileId: null,
                    profileProjectRelative: null,
                    profileProjectFullPath: null,
                    startupProjectFullPath: startupCsprojFull),
                McpResolveError: null);

        var resolvedProfile = preResolve.Profile;
        var readiness = preResolve.Readiness;
        if (!readiness.CanAttemptResolve)
            return null;

        if (readiness.Source == LaunchReadinessSource.Profile && resolvedProfile is { } launchProfile && !string.IsNullOrEmpty(readiness.SelectedProjectFullPath))
        {
            var (target, err) = await MsBuildDebugTargetResolver
                .TryResolveAsync(readiness.SelectedProjectFullPath, _dotnetRunner, launchProfile.Configuration)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(target))
                return DebugLaunchFromProfile.ToResolution(launchProfile, target);

            if (!string.IsNullOrEmpty(err))
                await ShowDebugInfoAsync("Стартовый проект", err).ConfigureAwait(false);
            return null;
        }

        if (!string.IsNullOrEmpty(readiness.SelectedProjectFullPath))
        {
            var (target, err) = await MsBuildDebugTargetResolver
                .TryResolveAsync(readiness.SelectedProjectFullPath, _dotnetRunner, LaunchProfilesStore.DefaultConfiguration)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(target))
                return new DebugLaunchResolution(target, null, null, null, OpenLaunchBrowser: false, LaunchUrl: null);

            if (!string.IsNullOrEmpty(err))
                await ShowDebugInfoAsync("Стартовый проект", err).ConfigureAwait(false);
        }

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

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return "# Error: workspace_root_unresolved.";

        var preResolve = LaunchPreResolvePipelineUnit.Default.Compose(
            sln,
            explicitProfileName: profileName,
            solutionDirectory: solutionDir,
            startupProjectFullPath: null);

        var resolvedProfile = preResolve.Profile;
        var readiness = preResolve.Readiness;

        if (!readiness.CanAttemptResolve || readiness.Source != LaunchReadinessSource.Profile || resolvedProfile is not { } launchProfile)
        {
            return preResolve.McpResolveError
                ?? LaunchMcpErrorFormatUnit.Default.FormatResolveFailure(
                    readiness,
                    explicitProfileName: profileName,
                    solutionDirectory: solutionDir);
        }

        var (target, err) = await MsBuildDebugTargetResolver
            .TryResolveAsync(readiness.SelectedProjectFullPath!, _dotnetRunner, launchProfile.Configuration, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrEmpty(target))
            return "# Error: " + (err ?? "msbuild_unresolved");

        var prg = mcpProgramArgs is { Count: > 0 } ? mcpProgramArgs : launchProfile.ProgramArgs;
        IReadOnlyDictionary<string, string>? env = DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(launchProfile);
        var launchResult = await DapDebug.LaunchAsync(
            workspacePath,
            target,
            netcoredbgPath,
            prg,
            env,
            launchProfile.WorkingDirectoryRelative,
            cancellationToken).ConfigureAwait(false);
        if (launchProfile.OpenLaunchBrowser)
            KestrelLaunchBrowser.TryOpenAfterLaunch(
                DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(launchProfile),
                launchProfile.LaunchUrl);
        return launchResult;
    }

}

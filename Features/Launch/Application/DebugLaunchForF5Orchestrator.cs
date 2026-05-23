#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Launch.Application;

/// <summary>
/// Pre-resolve launch profile + MSBuild для F5 из главного окна (профиль / стартовый <c>.csproj</c>).
/// </summary>
[ApplicationOrchestrator("debug-launch-f5-ui")]
public static class DebugLaunchForF5Orchestrator
{
    /// <summary>
    /// Перед DAP: эвристика стартового проекта, затем пайплайн <see cref="LaunchPreResolvePipelineUnit"/> и <see cref="MsBuildDebugTargetResolver"/>.
    /// </summary>
    public static async Task<DebugLaunchResolution?> TryResolveAsync(
        ObservableCollection<SolutionItem> solutionRoots,
        string? currentFilePath,
        string? selectedSolutionItemFullPath,
        Func<string?> getStartupCsprojFullPath,
        Action<string> applyStartupProject,
        string? solutionPath,
        string? solutionDirectory,
        IDotnetCommandRunner dotnetRunner,
        Func<string, string, Task> showDebugInfoAsync,
        CancellationToken cancellationToken = default)
    {
        ApplyInferredStartupIfNeeded(
            solutionRoots,
            currentFilePath,
            selectedSolutionItemFullPath,
            getStartupCsprojFullPath,
            applyStartupProject);

        var hasSolution = !string.IsNullOrEmpty(solutionPath);
        var hasWorkspaceRoot = !string.IsNullOrEmpty(solutionDirectory);

        var startupCsprojFull = LaunchProjectPathResolver.NormalizeExistingProjectFileFullPath(getStartupCsprojFullPath());
        var preResolve = hasSolution && hasWorkspaceRoot
            ? LaunchPreResolvePipelineUnit.Default.Compose(
                solutionPath!,
                explicitProfileName: null,
                solutionDirectory: solutionDirectory!,
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

        if (readiness.Source == LaunchReadinessSource.Profile && resolvedProfile is { } launchProfile &&
            !string.IsNullOrEmpty(readiness.SelectedProjectFullPath))
        {
            var (target, err) = await MsBuildDebugTargetResolver
                .TryResolveAsync(readiness.SelectedProjectFullPath, dotnetRunner, launchProfile.Configuration, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(target))
                return DebugLaunchFromProfile.ToResolution(launchProfile, target);

            if (!string.IsNullOrEmpty(err))
                await showDebugInfoAsync("Стартовый проект", err).ConfigureAwait(false);
            return null;
        }

        if (!string.IsNullOrEmpty(readiness.SelectedProjectFullPath))
        {
            var (target, err) = await MsBuildDebugTargetResolver
                .TryResolveAsync(
                    readiness.SelectedProjectFullPath,
                    dotnetRunner,
                    LaunchProfilesStore.DefaultConfiguration,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(target))
                return new DebugLaunchResolution(target, null, null, null, OpenLaunchBrowser: false, LaunchUrl: null);

            if (!string.IsNullOrEmpty(err))
                await showDebugInfoAsync("Стартовый проект", err).ConfigureAwait(false);
        }

        return null;
    }

    internal static void ApplyInferredStartupIfNeeded(
        ObservableCollection<SolutionItem> solutionRoots,
        string? currentFilePath,
        string? selectedSolutionItemFullPath,
        Func<string?> getStartupCsprojFullPath,
        Action<string> applyStartupProject)
    {
        if (StartupProjectDebugInferenceProjection.HasPersistedStartupPointingToExistingFile(getStartupCsprojFullPath()))
            return;

        var inferred = StartupProjectDebugInferenceProjection.TryInferCanonicalCsproj(
            solutionRoots,
            currentFilePath,
            selectedSolutionItemFullPath);
        if (!string.IsNullOrEmpty(inferred))
            applyStartupProject(inferred);
    }
}

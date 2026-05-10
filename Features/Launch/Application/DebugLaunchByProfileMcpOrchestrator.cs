#nullable enable
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Launch.Application;

/// <summary>
/// Режим B <c>debug_launch</c> (MCP): явный <paramref name="targetPath"/> или resolve по профилю / решению.
/// </summary>
[ComputingUnit("debug-launch-mcp-profile")]
public static class DebugLaunchByProfileMcpOrchestrator
{
    public static async Task<string> RunAsync(
        string workspacePath,
        string? targetPath,
        string? profileName,
        string? netcoredbgPath,
        IReadOnlyList<string>? mcpProgramArgs,
        IDotnetCommandRunner dotnetRunner,
        IdeDapDebugSession dapDebug,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(targetPath))
        {
            return await dapDebug.LaunchAsync(
                    workspacePath,
                    targetPath!,
                    netcoredbgPath,
                    mcpProgramArgs,
                    environment: null,
                    workingDirectoryOverride: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var sln = DebugWorkspacePath.TryResolveWorkspaceToSolutionPath(workspacePath);
        if (string.IsNullOrEmpty(sln))
            return "# Error: no_solution_in_workspace: укажи каталог с .sln или путь к .sln, либо target_path к .dll.";

        var solutionDir = BreakpointsFileService.GetWorkspaceRoot(sln);
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
            .TryResolveAsync(readiness.SelectedProjectFullPath!, dotnetRunner, launchProfile.Configuration, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrEmpty(target))
            return "# Error: " + (err ?? "msbuild_unresolved");

        var prg = mcpProgramArgs is { Count: > 0 } ? mcpProgramArgs : launchProfile.ProgramArgs;
        IReadOnlyDictionary<string, string>? env = DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(launchProfile);
        var launchResult = await dapDebug.LaunchAsync(
                workspacePath,
                target,
                netcoredbgPath,
                prg,
                env,
                launchProfile.WorkingDirectoryRelative,
                cancellationToken)
            .ConfigureAwait(false);
        if (launchProfile.OpenLaunchBrowser)
            KestrelLaunchBrowser.TryOpenAfterLaunch(
                DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(launchProfile),
                launchProfile.LaunchUrl);
        return launchResult;
    }
}

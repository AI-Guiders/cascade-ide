#nullable enable

using CascadeIDE.Contracts;

namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

/// <summary>
/// Мини-пайплайн pre-resolve для запуска: profile->project resolve + readiness + (опционально) MCP error message.
/// </summary>
[ComputingUnit]
public sealed class LaunchPreResolvePipelineUnit : ICockpitComputeUnit
{
    private readonly LaunchProfileProjectResolveUnit _profileResolve = LaunchProfileProjectResolveUnit.Default;
    private readonly LaunchReadinessUnit _readiness = LaunchReadinessUnit.Default;
    private readonly LaunchMcpErrorFormatUnit _mcpError = LaunchMcpErrorFormatUnit.Default;

    public static LaunchPreResolvePipelineUnit Default { get; } = new();

    private LaunchPreResolvePipelineUnit()
    {
    }

    public LaunchPreResolvePipelineSnapshot Compose(
        string solutionPath,
        string? explicitProfileName,
        string solutionDirectory,
        string? startupProjectFullPath)
    {
        var profileResolve = _profileResolve.Compose(solutionPath, explicitProfileName, solutionDirectory);
        var readiness = _readiness.Compose(
            hasSolutionPath: !string.IsNullOrWhiteSpace(solutionPath),
            hasWorkspaceRoot: !string.IsNullOrWhiteSpace(solutionDirectory),
            profileId: profileResolve.Profile?.ProfileId,
            profileProjectRelative: profileResolve.Profile?.ProjectRelativeToSolution,
            profileProjectFullPath: profileResolve.ProjectCsprojFullPath,
            startupProjectFullPath: startupProjectFullPath);

        return new LaunchPreResolvePipelineSnapshot(
            profileResolve.Profile,
            profileResolve.ProjectCsprojFullPath,
            readiness,
            readiness.CanAttemptResolve
                ? null
                : _mcpError.FormatResolveFailure(readiness, explicitProfileName, solutionDirectory));
    }
}

public readonly record struct LaunchPreResolvePipelineSnapshot(
    LaunchProfileData? Profile,
    string? ProfileProjectCsprojFullPath,
    LaunchReadinessSnapshot Readiness,
    string? McpResolveError) : ICockpitComputeUnitPayload;

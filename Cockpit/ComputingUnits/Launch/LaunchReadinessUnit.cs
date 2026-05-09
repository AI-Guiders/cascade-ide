#nullable enable

using CascadeIDE.Contracts;

namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

/// <summary>
/// CCU «готовность запуска» (pre-MSBuild): выбирает источник старта (профиль / startup project)
/// и фиксирует причину, если попытка разрешения цели невозможна.
/// </summary>
[ComputingUnit]
public sealed class LaunchReadinessUnit : ICockpitComputeUnit
{
    public static LaunchReadinessUnit Default { get; } = new();

    private LaunchReadinessUnit()
    {
    }

    public LaunchReadinessSnapshot Compose(
        bool hasSolutionPath,
        bool hasWorkspaceRoot,
        string? profileId,
        string? profileProjectRelative,
        string? profileProjectFullPath,
        string? startupProjectFullPath)
    {
        if (!hasSolutionPath)
            return LaunchReadinessSnapshot.NotReady("solution_missing");
        if (!hasWorkspaceRoot)
            return LaunchReadinessSnapshot.NotReady("workspace_root_unresolved");

        var hasProfileProject = !string.IsNullOrWhiteSpace(profileProjectFullPath);
        var hasStartupProject = !string.IsNullOrWhiteSpace(startupProjectFullPath);
        if (hasProfileProject)
        {
            return new LaunchReadinessSnapshot(
                CanAttemptResolve: true,
                Source: LaunchReadinessSource.Profile,
                ReasonCode: null,
                ProfileId: profileId,
                ProfileProjectRelative: profileProjectRelative,
                SelectedProjectFullPath: profileProjectFullPath);
        }

        if (hasStartupProject)
        {
            return new LaunchReadinessSnapshot(
                CanAttemptResolve: true,
                Source: LaunchReadinessSource.StartupProject,
                ReasonCode: null,
                ProfileId: profileId,
                ProfileProjectRelative: profileProjectRelative,
                SelectedProjectFullPath: startupProjectFullPath);
        }

        return LaunchReadinessSnapshot.NotReady(
            !string.IsNullOrWhiteSpace(profileId) ? "profile_project_not_found" : "startup_project_missing",
            profileId,
            profileProjectRelative);
    }
}

public enum LaunchReadinessSource
{
    None = 0,
    Profile = 1,
    StartupProject = 2
}

public readonly record struct LaunchReadinessSnapshot(
    bool CanAttemptResolve,
    LaunchReadinessSource Source,
    string? ReasonCode,
    string? ProfileId,
    string? ProfileProjectRelative,
    string? SelectedProjectFullPath) : ICockpitComputeUnitPayload
{
    public static LaunchReadinessSnapshot NotReady(
        string reasonCode,
        string? profileId = null,
        string? profileProjectRelative = null) =>
        new(
            CanAttemptResolve: false,
            Source: LaunchReadinessSource.None,
            ReasonCode: reasonCode,
            ProfileId: profileId,
            ProfileProjectRelative: profileProjectRelative,
            SelectedProjectFullPath: null);
}

#nullable enable

namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

/// <summary>
/// CCU «profile -> project path»: читает launch profile и пытается разрешить существующий .csproj.
/// Никаких UI-эффектов; только снимок для дальнейшей orchestration-логики.
/// </summary>
public sealed class LaunchProfileProjectResolveUnit : ICockpitComputeUnit
{
    public static LaunchProfileProjectResolveUnit Default { get; } = new();

    private LaunchProfileProjectResolveUnit()
    {
    }

    public LaunchProfileProjectResolveSnapshot Compose(
        string solutionPath,
        string? profileName,
        string solutionDirectory)
    {
        if (!LaunchProfilesStore.TryResolveProfileForLaunch(solutionPath, profileName, out var profile, out _))
            return LaunchProfileProjectResolveSnapshot.Empty;

        string? csprojFullPath = null;
        if (!string.IsNullOrWhiteSpace(profile.ProjectRelativeToSolution))
            _ = LaunchProjectPathResolver.TryGetExistingCsprojFullPath(solutionDirectory, profile.ProjectRelativeToSolution, out csprojFullPath);

        return new LaunchProfileProjectResolveSnapshot(profile, csprojFullPath);
    }
}

public readonly record struct LaunchProfileProjectResolveSnapshot(
    LaunchProfileData? Profile,
    string? ProjectCsprojFullPath) : ICockpitComputeUnitPayload
{
    public static LaunchProfileProjectResolveSnapshot Empty => new(Profile: null, ProjectCsprojFullPath: null);

    public bool HasProfile => Profile is not null;
}

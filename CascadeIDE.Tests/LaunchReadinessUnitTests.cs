using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.Launch;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LaunchReadinessUnitTests
{
    [Fact]
    public void Compose_prefers_profile_when_profile_project_exists()
    {
        var snapshot = LaunchReadinessUnit.Default.Compose(
            hasSolutionPath: true,
            hasWorkspaceRoot: true,
            profileId: "Web",
            profileProjectRelative: @"src\Web\Web.csproj",
            profileProjectFullPath: @"D:\repo\src\Web\Web.csproj",
            startupProjectFullPath: @"D:\repo\src\App\App.csproj");

        Assert.True(snapshot.CanAttemptResolve);
        Assert.Equal(LaunchReadinessSource.Profile, snapshot.Source);
        Assert.Equal("Web", snapshot.ProfileId);
        Assert.Equal(@"D:\repo\src\Web\Web.csproj", snapshot.SelectedProjectFullPath);
    }

    [Fact]
    public void Compose_falls_back_to_startup_project_when_profile_project_missing()
    {
        var snapshot = LaunchReadinessUnit.Default.Compose(
            hasSolutionPath: true,
            hasWorkspaceRoot: true,
            profileId: "Web",
            profileProjectRelative: @"src\Web\Web.csproj",
            profileProjectFullPath: null,
            startupProjectFullPath: @"D:\repo\src\App\App.csproj");

        Assert.True(snapshot.CanAttemptResolve);
        Assert.Equal(LaunchReadinessSource.StartupProject, snapshot.Source);
        Assert.Equal(@"D:\repo\src\App\App.csproj", snapshot.SelectedProjectFullPath);
    }

    [Fact]
    public void Compose_returns_not_ready_without_solution_or_workspace()
    {
        var noSolution = LaunchReadinessUnit.Default.Compose(
            hasSolutionPath: false,
            hasWorkspaceRoot: true,
            profileId: null,
            profileProjectRelative: null,
            profileProjectFullPath: null,
            startupProjectFullPath: null);
        Assert.False(noSolution.CanAttemptResolve);
        Assert.Equal("solution_missing", noSolution.ReasonCode);

        var noWorkspace = LaunchReadinessUnit.Default.Compose(
            hasSolutionPath: true,
            hasWorkspaceRoot: false,
            profileId: null,
            profileProjectRelative: null,
            profileProjectFullPath: null,
            startupProjectFullPath: null);
        Assert.False(noWorkspace.CanAttemptResolve);
        Assert.Equal("workspace_root_unresolved", noWorkspace.ReasonCode);
    }

    [Fact]
    public void LaunchReadinessUnit_default_and_snapshot_implement_ccu_contracts()
    {
        ICockpitComputeUnit unit = LaunchReadinessUnit.Default;
        Assert.NotNull(unit);

        ICockpitComputeUnitPayload payload = LaunchReadinessUnit.Default.Compose(
            hasSolutionPath: true,
            hasWorkspaceRoot: true,
            profileId: null,
            profileProjectRelative: null,
            profileProjectFullPath: null,
            startupProjectFullPath: @"D:\repo\src\App\App.csproj");
        Assert.NotNull(payload);
    }
}

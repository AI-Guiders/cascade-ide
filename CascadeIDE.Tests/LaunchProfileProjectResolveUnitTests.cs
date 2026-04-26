using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.Launch;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LaunchProfileProjectResolveUnitTests
{
    [Fact]
    public void Compose_returns_empty_when_profile_not_found()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-lpru-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var sln = Path.Combine(root, "App.sln");
            File.WriteAllText(sln, "");
            var snapshot = LaunchProfileProjectResolveUnit.Default.Compose(sln, "Missing", root);
            Assert.False(snapshot.HasProfile);
            Assert.Null(snapshot.ProjectCsprojFullPath);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Compose_returns_profile_and_resolved_project_when_exists()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-lpru-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var sln = Path.Combine(root, "App.sln");
            File.WriteAllText(sln, "");

            var projDir = Path.Combine(root, "src", "App");
            Directory.CreateDirectory(projDir);
            var csproj = Path.Combine(projDir, "App.csproj");
            File.WriteAllText(csproj, "<Project />");

            var launchTomlPath = LaunchProfilesStore.GetStorePath(sln);
            Directory.CreateDirectory(Path.GetDirectoryName(launchTomlPath)!);
            File.WriteAllText(
                launchTomlPath,
                """
                version = 1
                active_profile = "Default"

                [profiles.Default]
                project = "src/App/App.csproj"
                configuration = "Debug"
                """);

            var snapshot = LaunchProfileProjectResolveUnit.Default.Compose(sln, null, root);
            Assert.True(snapshot.HasProfile);
            Assert.NotNull(snapshot.Profile);
            Assert.Equal("Default", snapshot.Profile!.Value.ProfileId);
            Assert.Equal(Path.GetFullPath(csproj), snapshot.ProjectCsprojFullPath);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void LaunchProfileProjectResolveUnit_default_and_snapshot_implement_ccu_contracts()
    {
        ICockpitComputeUnit unit = LaunchProfileProjectResolveUnit.Default;
        Assert.NotNull(unit);

        ICockpitComputeUnitPayload payload = LaunchProfileProjectResolveSnapshot.Empty;
        Assert.NotNull(payload);
    }
}

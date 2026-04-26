using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.Launch;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LaunchPreResolvePipelineUnitTests
{
    [Fact]
    public void Compose_returns_profile_readiness_when_profile_project_exists()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-lprp-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var sln = Path.Combine(root, "App.sln");
            File.WriteAllText(sln, "");

            var projDir = Path.Combine(root, "src", "Web");
            Directory.CreateDirectory(projDir);
            var csproj = Path.Combine(projDir, "Web.csproj");
            File.WriteAllText(csproj, "<Project />");

            var launchTomlPath = LaunchProfilesStore.GetStorePath(sln);
            Directory.CreateDirectory(Path.GetDirectoryName(launchTomlPath)!);
            File.WriteAllText(
                launchTomlPath,
                """
                version = 1
                active_profile = "Web"

                [profiles.Web]
                project = "src/Web/Web.csproj"
                configuration = "Debug"
                """);

            var snap = LaunchPreResolvePipelineUnit.Default.Compose(
                sln,
                explicitProfileName: null,
                solutionDirectory: root,
                startupProjectFullPath: null);

            Assert.NotNull(snap.Profile);
            Assert.Equal(LaunchReadinessSource.Profile, snap.Readiness.Source);
            Assert.True(snap.Readiness.CanAttemptResolve);
            Assert.Null(snap.McpResolveError);
            Assert.Equal(Path.GetFullPath(csproj), snap.ProfileProjectCsprojFullPath);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Compose_produces_mcp_error_when_explicit_profile_not_found()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-lprp-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var sln = Path.Combine(root, "App.sln");
            File.WriteAllText(sln, "");

            var snap = LaunchPreResolvePipelineUnit.Default.Compose(
                sln,
                explicitProfileName: "Missing",
                solutionDirectory: root,
                startupProjectFullPath: null);

            Assert.False(snap.Readiness.CanAttemptResolve);
            Assert.Equal("# Error: profile_not_found: Missing", snap.McpResolveError);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void LaunchPreResolvePipelineUnit_default_and_snapshot_implement_ccu_contracts()
    {
        ICockpitComputeUnit unit = LaunchPreResolvePipelineUnit.Default;
        Assert.NotNull(unit);

        ICockpitComputeUnitPayload payload = new LaunchPreResolvePipelineSnapshot(
            Profile: null,
            ProfileProjectCsprojFullPath: null,
            Readiness: LaunchReadinessSnapshot.NotReady("x"),
            McpResolveError: "# Error: x");
        Assert.NotNull(payload);
    }
}

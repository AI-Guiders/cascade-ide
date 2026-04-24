using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LaunchProfilesStoreTests
{
    [Fact]
    public void Migrates_From_StartupProjectJson_To_Toml_And_Yields_Active_Project()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-launch-profiles-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var sln = Path.Combine(root, "Test.sln");
            File.WriteAllText(sln, "\n");

            var cide = Path.Combine(root, ".cascade-ide");
            Directory.CreateDirectory(cide);
            File.WriteAllText(
                Path.Combine(cide, "startup-project.json"),
                """{"StartupProjectRelativePath":"sub/P.csproj"}""" + "\n");

            Assert.True(LaunchProfilesStore.TryGetActiveProjectRelativePath(sln, out var rel, out var err), err);
            var expected = Path.Combine("sub", "P.csproj");
            Assert.Equal(expected, rel);
            Assert.True(File.Exists(Path.Combine(cide, LaunchProfilesStore.FileName)));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch { /* tmp cleanup best-effort */ }
        }
    }

    [Fact]
    public void Missing_Profile_Yields_profile_not_found()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-launch-bad-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var sln = Path.Combine(root, "A.sln");
            File.WriteAllText(sln, "\n");
            var cide = Path.Combine(root, ".cascade-ide");
            Directory.CreateDirectory(cide);
            File.WriteAllText(
                Path.Combine(cide, LaunchProfilesStore.FileName),
                "version = 1\nactive_profile = \"X\"\n[profiles.x]\nproject = \"P.csproj\"\n");

            Assert.False(LaunchProfilesStore.TryResolveProfileForLaunch(sln, "does_not_exist", out _, out var err));
            Assert.Contains("profile_not_found", err, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch { /* ignore */ }
        }
    }
}

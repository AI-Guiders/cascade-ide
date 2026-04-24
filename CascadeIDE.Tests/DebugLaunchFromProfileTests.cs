using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class DebugLaunchFromProfileTests
{
    [Fact]
    public void TryGetExistingCsproj_Existing_Returns_True()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-dlfp-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var fsPath = "sub" + Path.DirectorySeparatorChar + "A.csproj";
            var sub = Path.Combine(root, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "A.csproj"), "<Project />");

            Assert.True(
                DebugLaunchFromProfile.TryGetExistingCsprojFullPath(root, fsPath, out var full) &&
                full is { Length: > 0 } &&
                File.Exists(full!));
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
                // best-effort
            }
        }
    }

    [Fact]
    public void NonEmptyEnvironmentOrNull_Empty_Dictionary_Returns_Null()
    {
        var prof = new LaunchProfileData(
            "P",
            "a.csproj",
            LaunchProfilesStore.DefaultConfiguration,
            null,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal),
            false,
            null);
        Assert.Null(DebugLaunchFromProfile.NonEmptyEnvironmentOrNull(prof));
    }
}

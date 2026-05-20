using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAttachScopeTests
{
    [Fact]
    public void ResolveSolutionPath_prefers_live_over_session_meta()
    {
        var root = CreateTempDir();
        var slnx = Path.Combine(root, "App.slnx");
        File.WriteAllText(slnx, "");
        var resolved = IntercomAttachScope.ResolveSolutionPath(root, slnx, "Other.sln");
        Assert.Equal(slnx, resolved, ignoreCase: true);
    }

    [Fact]
    public void ResolveSolutionPath_uses_session_meta_when_live_empty()
    {
        var root = CreateTempDir();
        var slnx = Path.Combine(root, "CascadeIDE.slnx");
        File.WriteAllText(slnx, "");

        var resolved = IntercomAttachScope.ResolveSolutionPath(root, null, "CascadeIDE.slnx");
        Assert.Equal(slnx, resolved, ignoreCase: true);
    }

    [Fact]
    public void TryDiscoverSolutionPathRelative_finds_single_slnx()
    {
        var root = CreateTempDir();
        File.WriteAllText(Path.Combine(root, "Only.slnx"), "");

        var rel = IntercomAttachScope.TryDiscoverSolutionPathRelative(root);
        Assert.Equal("Only.slnx", rel);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "intercom-scope-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

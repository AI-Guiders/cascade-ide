#nullable enable
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassWorkspaceHealthGlanceTests
{
    [Fact]
    public void Format_missing_root_marks_MISSING()
    {
        var body = GlassWorkspaceHealthGlance.Format(
            new GlassWorkspaceHealthGlance.WorkspaceFsStatus(
                RootPath: @"D:\missing-ws",
                RootExists: false,
                HasGit: false,
                SlnPath: null,
                HasCascadeIdeDir: false));

        Assert.Contains("WorkspaceHealth glance · MISSING", body);
        Assert.Contains("root · missing", body);
        Assert.Contains("■ Glass FS status", body);
        Assert.Contains("□ Avalonia IdeHealth", body);
    }

    [Fact]
    public void Format_with_sln_marks_READY()
    {
        var body = GlassWorkspaceHealthGlance.Format(
            new GlassWorkspaceHealthGlance.WorkspaceFsStatus(
                RootPath: @"D:\ws",
                RootExists: true,
                HasGit: true,
                SlnPath: @"D:\ws\CascadeIDE.sln",
                HasCascadeIdeDir: true));

        Assert.Contains("WorkspaceHealth glance · READY", body);
        Assert.Contains("sln · CascadeIDE.sln", body);
        Assert.Contains("git · yes", body);
        Assert.Contains(".cascade-ide · yes", body);
    }

    [Fact]
    public void Format_root_without_sln_or_git_marks_THIN()
    {
        var body = GlassWorkspaceHealthGlance.Format(
            new GlassWorkspaceHealthGlance.WorkspaceFsStatus(
                RootPath: @"D:\empty",
                RootExists: true,
                HasGit: false,
                SlnPath: null,
                HasCascadeIdeDir: false));

        Assert.Contains("WorkspaceHealth glance · THIN", body);
        Assert.Contains("no .sln / .git", body);
    }

    [Fact]
    public void TryProbe_null_root_still_returns_instrument_status()
    {
        var probe = GlassWorkspaceHealthGlance.TryProbe(null);
        Assert.NotNull(probe);
        var chips = GlassGlanceCards.BuildWorkspaceHealth(probe.Value);
        Assert.Contains(chips, c => c.Label == "LEVEL");
    }
}

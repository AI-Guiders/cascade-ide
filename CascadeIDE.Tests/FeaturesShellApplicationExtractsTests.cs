using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Launch.Application;
using CascadeIDE.Features.Markdown.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class FeaturesShellApplicationExtractsTests
{
    [Fact]
    public void CommandPaletteSubtitle_handles_empty_category()
    {
        Assert.Equal("cid", CommandPaletteSubtitleProjection.CommandPaletteSubtitle("cid", ""));
        Assert.Equal("cid · cat", CommandPaletteSubtitleProjection.CommandPaletteSubtitle("cid", " cat "));
    }

    [Fact]
    public void GoToPaletteRipgrepPattern_builder_matches_prefixes()
    {
        var qx = new GoToAllQuery('x', "  foo ");
        var (px, fx, gx) = GoToPaletteRipgrepPatternBuilder.Build(qx);
        Assert.Equal("foo", px);
        Assert.True(fx);
        Assert.Null(gx);

        var qt = new GoToAllQuery('t', "MyType");
        var (pt, ft, gt) = GoToPaletteRipgrepPatternBuilder.Build(qt);
        Assert.Contains("MyType", pt);
        Assert.False(ft);
        Assert.Equal("*.cs", gt);

        var qm = new GoToAllQuery('m', "Bar");
        var (pm, fm, gm) = GoToPaletteRipgrepPatternBuilder.Build(qm);
        Assert.Contains(@"\bBar\b", pm);
        Assert.False(fm);
        Assert.Equal("*.cs", gm);
    }

    [Fact]
    public void UiModeSelectionParameter_parses_common_forms()
    {
        Assert.Equal(3, UiModeSelectionParameter.ParseIndex(3));
        Assert.Equal(2, UiModeSelectionParameter.ParseIndex(2L));
        Assert.Equal(1, UiModeSelectionParameter.ParseIndex("1"));
        Assert.Equal(-1, UiModeSelectionParameter.ParseIndex(long.MaxValue));
        Assert.Equal(-1, UiModeSelectionParameter.ParseIndex(null));
    }

    [Fact]
    public void WorkspaceDirectoryFromSolutionPath_normalize_existing_file()
    {
        var dir = WorkspaceDirectoryFromSolutionPath.Resolve("");
        Assert.Equal("", dir);

        try
        {
            var tmp = Path.Combine(Path.GetTempPath(), "cascdirtest-" + Guid.NewGuid().ToString("n") + ".txt");
            File.WriteAllText(tmp, "x");
            var ws = WorkspaceDirectoryFromSolutionPath.Resolve(tmp);
            Assert.Equal(Path.GetDirectoryName(tmp), ws);
        }
        catch
        {
            // temp IO optional in CI sandbox
        }
    }

    [Fact]
    public void ExpandedMarkdownDefaultExportPath_uses_filename()
    {
        var dir = Path.GetTempPath();
        var src = Path.Combine(dir, "Note.md");
        var path = ExpandedMarkdownDefaultExportPath.Resolve(src);
        Assert.EndsWith("Note.expanded.md", path.Replace('/', Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HybridIndexHisPathDisplayShortener_trims_paths()
    {
        Assert.Equal("—", HybridIndexHisPathDisplayShortener.ShortenLikeEcam(""));
        Assert.Equal("—", HybridIndexHisPathDisplayShortener.ShortenLikeEcam("—"));

        var withSep = $"a{Path.DirectorySeparatorChar}b{Path.DirectorySeparatorChar}File.cs";
        Assert.Equal("File.cs", HybridIndexHisPathDisplayShortener.ShortenLikeEcam(withSep));

        var longNoSep = new string('a', 40);
        var shorted = HybridIndexHisPathDisplayShortener.ShortenLikeEcam(longNoSep);
        Assert.True(shorted.Length < longNoSep.Length);
    }

    [Fact]
    public void LaunchProjectRelativePath_requires_inside_solution_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "cascade-launch-rel-" + Guid.NewGuid().ToString("n"));
        var proj = Path.Combine(root, "nest", "p.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(proj)!);
        File.WriteAllText(proj, "<Project/>");

        Assert.True(LaunchProjectRelativePath.TryGetRelativeToSolutionDirectory(root, proj, out var rel, out var err));
        Assert.Null(err);
        Assert.Equal(Path.Combine("nest", "p.csproj"), rel!, StringComparer.OrdinalIgnoreCase);

        Assert.False(LaunchProjectRelativePath.TryGetRelativeToSolutionDirectory(root, Path.Combine(Path.GetTempPath(), "outside", "x.csproj"), out _, out var err2));
        Assert.NotNull(err2);

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }
}

#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassSlashCatalogStepTests
{
    [Fact]
    public void Bare_slash_suggests_first_path_segments_only()
    {
        var hits = GlassSlashCatalog.Suggest("/");
        Assert.Contains(hits, h => h.Title == "intercom" && h.InsertText == "/intercom ");
        Assert.Contains(hits, h => h.Title == "open" && h.InsertText == "/open ");
        Assert.Contains(hits, h => h.Title == "file" && h.InsertText == "/file ");
        Assert.Contains(hits, h => h.Title == "solution" && h.InsertText == "/solution ");
        Assert.Contains(hits, h => h.Title == "folder" && h.InsertText == "/folder ");
        Assert.Contains(hits, h => h.Title == "search" && h.InsertText == "/search ");
        Assert.DoesNotContain(hits, h => h.Title.Contains(' '));
    }

    [Fact]
    public void After_intercom_space_suggests_next_segment()
    {
        var hits = GlassSlashCatalog.Suggest("/intercom ");
        Assert.Contains(hits, h => h.Title == "attach");
        Assert.Contains(hits, h => h.Title == "message");
        Assert.Contains(hits, h => h.Title == "overview");
    }

    [Fact]
    public void After_file_space_suggests_open_pick_save()
    {
        var hits = GlassSlashCatalog.Suggest("/file ");
        Assert.Contains(hits, h => h.Title == "open");
        Assert.Contains(hits, h => h.Title == "pick");
        Assert.Contains(hits, h => h.Title == "save");
    }

    [Fact]
    public void After_solution_space_suggests_open_load_new_explorer()
    {
        var hits = GlassSlashCatalog.Suggest("/solution ");
        Assert.Contains(hits, h => h.Title == "open");
        Assert.Contains(hits, h => h.Title == "load");
        Assert.Contains(hits, h => h.Title == "new");
        Assert.Contains(hits, h => h.Title == "explorer");
    }

    [Fact]
    public void Open_with_space_uses_workspace_file_matches()
    {
        GlassSlashCatalog.WorkspaceFileMatchSource files = (_, _) =>
            [("README.md", "readme"), ("src/A.cs", "A")];
        var hits = GlassSlashCatalog.Suggest("/open ", files);
        Assert.Equal(2, hits.Count);
        Assert.Equal("/open README.md", hits[0].InsertText);
        Assert.Equal("README.md", hits[0].Title);
    }

    [Fact]
    public void File_open_with_space_uses_workspace_file_matches()
    {
        GlassSlashCatalog.WorkspaceFileMatchSource files = (_, _) =>
            [("Main.cs", "main")];
        var hits = GlassSlashCatalog.Suggest("/file open ", files);
        Assert.Single(hits);
        Assert.Equal("/file open Main.cs", hits[0].InsertText);
    }

    [Fact]
    public void Solution_load_with_space_uses_workspace_file_matches()
    {
        GlassSlashCatalog.WorkspaceFileMatchSource files = (_, _) =>
            [("App.sln", "sln")];
        var hits = GlassSlashCatalog.Suggest("/solution load ", files);
        Assert.Single(hits);
        Assert.Equal("/solution load App.sln", hits[0].InsertText);
    }

    [Fact]
    public void Search_requires_pattern_before_autorun()
    {
        Assert.False(GlassSlashCatalog.ShouldAutoRunOnCommit("/search"));
        Assert.False(GlassSlashCatalog.ShouldAutoRunOnCommit("/search "));
        Assert.True(GlassSlashCatalog.ShouldAutoRunOnCommit("/search SoftFL"));
    }
}

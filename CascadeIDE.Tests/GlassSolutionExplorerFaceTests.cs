using CascadeIDE.Models;
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassSolutionExplorerFaceTests
{
    [Theory]
    [InlineData("SolutionExplorer", true)]
    [InlineData("solutionexplorer", true)]
    [InlineData("Git", false)]
    [InlineData(null, false)]
    public void PreferTreeHost_only_SE(string? page, bool expect) =>
        Assert.Equal(expect, GlassSolutionExplorerFace.PreferTreeHost(page));

    [Fact]
    public void ResolveItems_null_root_binds_empty_Face_placeholder_not_peel()
    {
        var rows = GlassSolutionExplorerFace.ResolveItems(null);
        Assert.Single(rows);
        Assert.Equal(GlassSolutionExplorerFace.EmptyTitle, rows[0].Title);
        Assert.Null(rows[0].FullPath);
    }

    [Fact]
    public void ResolveItems_with_children_binds_children_and_expands_project_roots()
    {
        var root = SolutionItem.CreateSolution("S", "S.sln");
        var proj = SolutionItem.CreateProject("P", "P.csproj");
        var file = SolutionItem.CreateFile("a.cs", "a.cs");
        proj.Children.Add(file);
        root.Children.Add(proj);

        var rows = GlassSolutionExplorerFace.ResolveItems(root);

        Assert.Same(root.Children, rows);
        Assert.True(proj.IsExpanded);
    }

    [Fact]
    public void ResolveItems_standalone_existing_file_binds_root()
    {
        var path = Path.Combine(Path.GetTempPath(), "glass-se-face-" + Guid.NewGuid().ToString("N") + ".cs");
        File.WriteAllText(path, "// face");
        try
        {
            var root = SolutionItem.CreateFile(Path.GetFileName(path), path);
            var rows = GlassSolutionExplorerFace.ResolveItems(root);
            Assert.Single(rows);
            Assert.Same(root, rows[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SoftOrgan_does_not_claim_SE_for_peel_overlay()
    {
        Assert.Null(SoftOrganMfdGlance.TryOrganIdForMfdPage(GlassSolutionExplorerFace.MfdPage));
    }

    [Fact]
    public void MfdBody_source_does_not_route_SE_to_Avalonia_FormatMfdStub()
    {
        // Regression: empty SE used FormatMfdStub("… Avalonia · SolutionExplorerView …") peel.
        var path = FindRepoFile("CDP.GlassCockpit.Windows", "MainWindow.MfdBody.cs");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("\"SolutionExplorer\" => FormatMfdStub", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SolutionExplorerView", text, StringComparison.Ordinal);
        Assert.Contains("PreferTree", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EditorSurface_source_shows_SE_tree_without_HasRows_gate()
    {
        // Regression: showSe required SolutionExplorerHasRows → empty = Avalonia peel visible.
        var path = FindRepoFile("CDP.GlassCockpit.Windows", "MainWindow.EditorSurface.cs");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("SolutionExplorerHasRows(MfdSolutionExplorerTree)", text, StringComparison.Ordinal);
        Assert.Contains("SolutionExplorer", text, StringComparison.Ordinal);
    }

    static string FindRepoFile(string folder, string file)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, folder, file);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine("..", "..", "..", "..", folder, file);
    }
}

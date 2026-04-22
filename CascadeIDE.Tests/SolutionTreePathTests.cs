using CascadeIDE.Features.Documents;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Регрессия: <see cref="Path.GetFullPath"/> на кривых строках в дереве решения не должен ронять синхронизацию выбора / открытие из карты кода.
/// </summary>
public sealed class SolutionTreePathTests
{
    [Fact]
    public void TryGetFullPath_embedded_null_returns_false_without_throwing()
    {
        var bad = "a\0b\\file.cs";
        Assert.False(SolutionTreePath.TryGetFullPath(bad, out var full));
        Assert.Equal("", full);
    }

    [Fact]
    public void TryGetFullPath_valid_temp_directory_matches_path_get_full_path()
    {
        var tmp = Path.GetTempPath();
        Assert.True(SolutionTreePath.TryGetFullPath(tmp, out var full));
        Assert.Equal(Path.GetFullPath(tmp), full);
    }

    [Fact]
    public void FindItemByFullPath_skips_unnormalizable_nodes_and_finds_matching_file()
    {
        var goodPath = Path.Combine(Path.GetTempPath(), "cascade_solution_tree_test_" + Guid.NewGuid().ToString("n") + ".cs");
        File.WriteAllText(goodPath, "//");
        try
        {
            var goodNorm = Path.GetFullPath(goodPath);
            var root = SolutionItem.CreateSolution("s", @"C:\x.sln");
            root.Children.Add(SolutionItem.CreateFile("bad-null", "a\0broken.cs"));
            root.Children.Add(SolutionItem.CreateFile("good", goodPath));

            var found = SolutionTreePath.FindItemByFullPath(new[] { root }, goodNorm);
            Assert.NotNull(found);
            Assert.Equal("good", found.Title);
            Assert.Equal(goodNorm, Path.GetFullPath(found.FullPath!));
        }
        finally
        {
            try
            {
                File.Delete(goodPath);
            }
            catch
            {
                // ignore
            }
        }
    }

    [Fact]
    public void FindItemByFullPath_returns_null_when_only_broken_paths()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\x.sln");
        root.Children.Add(SolutionItem.CreateFile("bad", "x\0y.cs"));

        var found = SolutionTreePath.FindItemByFullPath(new[] { root }, Path.GetFullPath(Path.GetTempPath()));
        Assert.Null(found);
    }
}

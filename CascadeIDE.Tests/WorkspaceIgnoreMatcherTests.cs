using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceIgnoreMatcherTests
{
    [Fact]
    public void Folder_workspace_respects_gitignore_and_cascadeignore()
    {
        WorkspaceIgnoreMatcher.ClearCacheForTests();
        var tmp = Directory.CreateTempSubdirectory("cascade_ws_ignore_");
        try
        {
            File.WriteAllText(Path.Combine(tmp.FullName, ".gitignore"), "*.tmp\n");
            File.WriteAllText(Path.Combine(tmp.FullName, ".cascadeignore"), "*.bak\n");
            File.WriteAllText(Path.Combine(tmp.FullName, "keep.cs"), "//");
            File.WriteAllText(Path.Combine(tmp.FullName, "x.tmp"), "");
            File.WriteAllText(Path.Combine(tmp.FullName, "y.bak"), "");

            var root = FolderWorkspaceTreeBuilder.TryBuild(tmp.FullName, out var err);
            Assert.Null(err);
            Assert.NotNull(root);
            var names = CollectFileNames(root).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("keep.cs", names);
            Assert.DoesNotContain("x.tmp", names);
            Assert.DoesNotContain("y.bak", names);
        }
        finally
        {
            try
            {
                Directory.Delete(tmp.FullName, recursive: true);
            }
            catch
            {
                // ignore
            }

            WorkspaceIgnoreMatcher.ClearCacheForTests();
        }
    }

    [Fact]
    public void GetOrCreate_applies_gitignore_to_paths()
    {
        WorkspaceIgnoreMatcher.ClearCacheForTests();
        var tmp = Directory.CreateTempSubdirectory("cascade_matcher_");
        try
        {
            File.WriteAllText(Path.Combine(tmp.FullName, ".gitignore"), "*.tmp\n");
            var m = WorkspaceIgnoreMatcher.GetOrCreate(tmp.FullName);
            Assert.True(m.IsIgnored(Path.Combine(tmp.FullName, "a.tmp")));
            Assert.False(m.IsIgnored(Path.Combine(tmp.FullName, "a.cs")));
        }
        finally
        {
            try
            {
                Directory.Delete(tmp.FullName, recursive: true);
            }
            catch
            {
                // ignore
            }

            WorkspaceIgnoreMatcher.ClearCacheForTests();
        }
    }

    [Fact]
    public void GetOrCreate_negation_unignores_in_same_gitignore()
    {
        WorkspaceIgnoreMatcher.ClearCacheForTests();
        var tmp = Directory.CreateTempSubdirectory("cascade_matcher_neg_");
        try
        {
            File.WriteAllText(
                Path.Combine(tmp.FullName, ".gitignore"),
                """
                *.tmp
                !keep.tmp

                """);
            var m = WorkspaceIgnoreMatcher.GetOrCreate(tmp.FullName);
            Assert.False(m.IsIgnored(Path.Combine(tmp.FullName, "keep.tmp")), "last match !keep.tmp");
            Assert.True(m.IsIgnored(Path.Combine(tmp.FullName, "other.tmp")));
        }
        finally
        {
            try
            {
                Directory.Delete(tmp.FullName, recursive: true);
            }
            catch
            {
                // ignore
            }

            WorkspaceIgnoreMatcher.ClearCacheForTests();
        }
    }

    [Fact]
    public void GetOrCreate_nested_gitignore_negation_unignores_file()
    {
        WorkspaceIgnoreMatcher.ClearCacheForTests();
        var tmp = Directory.CreateTempSubdirectory("cascade_matcher_nested_");
        try
        {
            var sub = Path.Combine(tmp.FullName, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(tmp.FullName, ".gitignore"), "*.tmp\n");
            File.WriteAllText(Path.Combine(sub, ".gitignore"), "!keep.tmp\n");
            File.WriteAllText(Path.Combine(sub, "keep.tmp"), "");
            File.WriteAllText(Path.Combine(sub, "drop.tmp"), "");

            var m = WorkspaceIgnoreMatcher.GetOrCreate(tmp.FullName);
            Assert.False(m.IsIgnored(Path.Combine(sub, "keep.tmp")), "nested !keep.tmp after root *.tmp");
            Assert.True(m.IsIgnored(Path.Combine(sub, "drop.tmp")));
        }
        finally
        {
            try
            {
                Directory.Delete(tmp.FullName, recursive: true);
            }
            catch
            {
                // ignore
            }

            WorkspaceIgnoreMatcher.ClearCacheForTests();
        }
    }

    [Fact]
    public void Folder_workspace_nested_gitignore_shows_unignored_tmp()
    {
        WorkspaceIgnoreMatcher.ClearCacheForTests();
        var tmp = Directory.CreateTempSubdirectory("cascade_ws_nested_");
        try
        {
            var sub = Path.Combine(tmp.FullName, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(tmp.FullName, ".gitignore"), "*.tmp\n");
            File.WriteAllText(Path.Combine(sub, ".gitignore"), "!keep.tmp\n");
            File.WriteAllText(Path.Combine(tmp.FullName, "gone.tmp"), "");
            File.WriteAllText(Path.Combine(sub, "keep.tmp"), "");
            File.WriteAllText(Path.Combine(sub, "gone2.tmp"), "");
            File.WriteAllText(Path.Combine(tmp.FullName, "ok.cs"), "//");

            var root = FolderWorkspaceTreeBuilder.TryBuild(tmp.FullName, out var err);
            Assert.Null(err);
            Assert.NotNull(root);
            var names = CollectFileNames(root).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("ok.cs", names);
            Assert.Contains("keep.tmp", names);
            Assert.DoesNotContain("gone.tmp", names);
            Assert.DoesNotContain("gone2.tmp", names);
        }
        finally
        {
            try
            {
                Directory.Delete(tmp.FullName, recursive: true);
            }
            catch
            {
                // ignore
            }

            WorkspaceIgnoreMatcher.ClearCacheForTests();
        }
    }

    private static IEnumerable<string> CollectFileNames(SolutionItem node)
    {
        if (node.FullPath is { } p && File.Exists(p))
            yield return Path.GetFileName(p);
        foreach (var c in node.Children)
        {
            foreach (var n in CollectFileNames(c))
                yield return n;
        }
    }
}

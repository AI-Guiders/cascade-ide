using CascadeIDE.Features.Agent.Environment;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentL0CsScopeParserTests
{
    [Fact]
    public void Merge_git_name_only_keeps_cs_and_order()
    {
        var merged = AgentL0CsScopeParser.MergeGitNameOnlyOutputs(
            "a.cs\nmisc.txt\nsubdir/b.cs",
            "subdir/b.cs\nc.cs");

        Assert.Equal(["a.cs", "subdir/b.cs", "c.cs"], merged);
    }

    [Fact]
    public void TryResolve_rejects_traversal_or_outside_root()
    {
        var ws = OperatingSystem.IsWindows() ? @"C:\demo\repo" : "/tmp/demo/repo";
        try
        {
            Directory.CreateDirectory(ws);
            var okCs = Path.Combine(ws, "X.cs");
            File.WriteAllText(okCs, "//");

            Assert.True(AgentL0CsScopeParser.TryResolveWorkspaceCs(ws, "X.cs", out var fullOk));
            Assert.Equal(Path.GetFullPath(okCs), fullOk);

            var parent = Directory.GetParent(ws)!.FullName;
            var escaped = Path.Combine(parent, "EscapedOutside.cs");
            File.WriteAllText(escaped, "//");
            Assert.False(AgentL0CsScopeParser.TryResolveWorkspaceCs(
                ws,
                Path.Combine("..", "EscapedOutside.cs").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar),
                out _));

            Directory.CreateDirectory(Path.Combine(ws, "nested"));
            var nestedCs = Path.Combine(ws, "nested", "z.cs");
            File.WriteAllText(nestedCs, "//");
            Assert.True(AgentL0CsScopeParser.TryResolveWorkspaceCs(ws, "nested/z.cs", out var zn));
            Assert.Equal(Path.GetFullPath(nestedCs), zn);
        }
        finally
        {
            try { Directory.Delete(ws, recursive: true); }
            catch { /* best-effort */ }
            try
            {
                var parent = OperatingSystem.IsWindows() ? @"C:\demo" : "/tmp/demo";
                var escaped = Path.Combine(parent, "EscapedOutside.cs");
                if (File.Exists(escaped))
                    File.Delete(escaped);
            }
            catch { /* best-effort */ }
        }
    }

    [Theory]
    [InlineData("open_tabs", false)]
    [InlineData("open_tabs_and_git_dirty_cs", true)]
    [InlineData("OPEN_TABS_AND_GIT_DIRTY_CS", true)]
    public void IncludesGitDirtyWorktreeCs_recognizes_scope(string scope, bool expected) =>
        Assert.Equal(expected, AgentL0CsScopeParser.IncludesGitDirtyWorktreeCs(scope));
}

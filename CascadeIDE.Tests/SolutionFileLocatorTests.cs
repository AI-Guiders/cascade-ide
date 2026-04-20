using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionFileLocatorTests
{
    [Fact]
    public void TryFindSolutionForSourceFile_WalksUpToSln()
    {
        var root = Path.Combine(Path.GetTempPath(), "cascade-slnfind-" + Guid.NewGuid().ToString("n"));
        var src = Path.Combine(root, "src", "Lib");
        Directory.CreateDirectory(src);
        var sln = Path.Combine(root, "App.sln");
        File.WriteAllText(sln, "Microsoft Visual Studio Solution File, Format Version 12.00\n");
        var cs = Path.Combine(src, "F.cs");
        File.WriteAllText(cs, "//");
        try
        {
            var found = SolutionFileLocator.TryFindSolutionForSourceFile(cs);
            Assert.NotNull(found);
            Assert.Equal(Path.GetFullPath(sln), Path.GetFullPath(found));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* temp */ }
        }
    }

    [Fact]
    public void TryFindSolutionForSourceFile_NoSolution_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "cascade-slnfind-none-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        var cs = Path.Combine(root, "X.cs");
        File.WriteAllText(cs, "//");
        try
        {
            Assert.Null(SolutionFileLocator.TryFindSolutionForSourceFile(cs));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void NeedsLoadSolutionBeforeBreakpoint_SamePath_ReturnsFalse()
    {
        var sln = @"C:\repo\A.sln";
        Assert.False(SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(sln, @"C:\repo\A.sln"));
    }

    [Fact]
    public void NeedsLoadSolutionBeforeBreakpoint_WhenFoundFileExists_MatchesContract()
    {
        var root = Path.Combine(Path.GetTempPath(), "cascade-needload-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        var slnA = Path.Combine(root, "A.sln");
        var slnB = Path.Combine(root, "B.sln");
        File.WriteAllText(slnA, "");
        File.WriteAllText(slnB, "");
        try
        {
            Assert.True(SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(slnA, null));
            Assert.True(SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(slnA, ""));
            Assert.False(SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(slnA, slnA));
            Assert.True(SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(slnA, slnB));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void NeedsLoadSolutionBeforeBreakpoint_MissingFile_ReturnsFalse()
    {
        Assert.False(SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(@"C:\no\such\file.sln", null));
    }
}

using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpSolutionTreeTests
{
    [Theory]
    [InlineData(@"D:\repo\proj\obj\Debug\net10.0\Foo.cs", true)]
    [InlineData(@"D:/repo/proj/obj/x/Foo.cs", true)]
    [InlineData(@"D:\repo\proj\bin\Debug\a.dll", true)]
    [InlineData(@"D:\repo\proj\Services\Foo.cs", false)]
    [InlineData(@"D:\repo\proj\objections\Note.cs", false)]
    public void IsBuildArtifactPath_Classifies_Obj_Bin_Segments(string path, bool expected)
    {
        Assert.Equal(expected, McpSolutionTree.IsBuildArtifactPath(path));
    }

    [Fact]
    public void ResolveOwningProjectPath_Picks_Nearest_Csproj_On_Disk()
    {
        var root = FindCascadeIdeRoot();
        var mainCsproj = Path.Combine(root, "CascadeIDE.csproj");
        var testsCsproj = Path.Combine(root, "CascadeIDE.Tests", "CascadeIDE.Tests.csproj");
        var contractsCsproj = Path.Combine(root, "CascadeIDE.Contracts", "CascadeIDE.Contracts.csproj");

        var mainFile = Path.Combine(root, "Features", "Workspace", "Application", "McpSolutionTree.cs");
        var testFile = Path.Combine(root, "CascadeIDE.Tests", "McpSolutionTreeTests.cs");
        var contractsFile = Path.Combine(root, "CascadeIDE.Contracts", "ApiStability.cs");

        Assert.True(File.Exists(mainFile), mainFile);
        Assert.True(File.Exists(testFile), testFile);
        Assert.True(File.Exists(contractsFile), contractsFile);

        Assert.Equal(CanonicalFilePath.Normalize(mainCsproj), CanonicalFilePath.Normalize(McpSolutionTree.ResolveOwningProjectPath(mainFile)!));
        Assert.Equal(CanonicalFilePath.Normalize(testsCsproj), CanonicalFilePath.Normalize(McpSolutionTree.ResolveOwningProjectPath(testFile)!));
        Assert.Equal(CanonicalFilePath.Normalize(contractsCsproj), CanonicalFilePath.Normalize(McpSolutionTree.ResolveOwningProjectPath(contractsFile)!));
    }

    private static string FindCascadeIdeRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var csproj = Path.Combine(dir.FullName, "CascadeIDE.csproj");
            if (File.Exists(csproj))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("CascadeIDE.csproj not found above test output directory.");
    }
}

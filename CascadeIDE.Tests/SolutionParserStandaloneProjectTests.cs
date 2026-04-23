using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionParserStandaloneProjectTests
{
    [Fact]
    public void Load_CsprojPath_BuildsTreeWithSingleProjectChild()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade_standalone_proj_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var csPath = Path.Combine(dir, "Lib.cs");
            File.WriteAllText(csPath, "// file");
            var csproj = Path.Combine(dir, "Standalone.csproj");
            File.WriteAllText(csproj, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """);

            var root = SolutionParser.Load(csproj, out var err);
            Assert.Null(err);
            Assert.NotNull(root);
            Assert.Equal("Standalone", root!.Title);
            Assert.Equal(csproj, root.FullPath);
            Assert.Single(root.Children);
            var proj = root.Children[0];
            Assert.Equal("Standalone.csproj", proj.Title);
            Assert.Equal(csproj, proj.FullPath);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* test temp */ }
        }
    }
}

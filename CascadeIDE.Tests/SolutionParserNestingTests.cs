using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public class SolutionParserNestingTests
{
    [Fact]
    public void SdkGlob_Nests_PartialCs_UnderLongestExistingStem()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade_sol_nest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "ViewModels"));
        try
        {
            File.WriteAllText(Path.Combine(dir, "Nest.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """);

            File.WriteAllText(Path.Combine(dir, "ViewModels", "Root.cs"), "// a");
            File.WriteAllText(Path.Combine(dir, "ViewModels", "Root.Part.cs"), "// b");

            var slnPath = Path.Combine(dir, "Nest.sln");
            File.WriteAllText(slnPath, """
                Microsoft Visual Studio Solution File, Format Version 12.00
                # Visual Studio Version 17
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Nest", "Nest.csproj", "{A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1}"
                EndProject
                Global
                	GlobalSection(SolutionConfigurationPlatforms) = preSolution
                		Debug|Any CPU = Debug|Any CPU
                	EndGlobalSection
                	GlobalSection(ProjectConfigurationPlatforms) = postSolution
                		{A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                		{A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1}.Debug|Any CPU.Build.0 = Debug|Any CPU
                	EndGlobalSection
                EndGlobal
                """);

            var root = SolutionParser.Load(slnPath, out var err);
            Assert.Null(err);
            Assert.NotNull(root);

            var proj = root!.Children.FirstOrDefault(c => c.Title.Equals("Nest.csproj", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(proj);

            var rootFile = FindFileByTitle(proj!, "Root.cs");
            Assert.NotNull(rootFile);
            Assert.Contains(rootFile!.Children, c => c.Title.Equals("Root.Part.cs", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    [Fact]
    public void DependentUpon_InCsproj_OverridesHeuristic()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade_sol_dep_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "ViewModels"));
        try
        {
            File.WriteAllText(Path.Combine(dir, "Dep.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Update="ViewModels\\Other.cs">
                      <DependentUpon>ViewModels\\Anchor.cs</DependentUpon>
                    </Compile>
                  </ItemGroup>
                </Project>
                """);

            File.WriteAllText(Path.Combine(dir, "ViewModels", "Anchor.cs"), "// a");
            File.WriteAllText(Path.Combine(dir, "ViewModels", "Other.cs"), "// b");

            var slnPath = Path.Combine(dir, "Dep.sln");
            File.WriteAllText(slnPath, """
                Microsoft Visual Studio Solution File, Format Version 12.00
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Dep", "Dep.csproj", "{B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2}"
                EndProject
                Global
                	GlobalSection(SolutionConfigurationPlatforms) = preSolution
                		Debug|Any CPU = Debug|Any CPU
                	EndGlobalSection
                	GlobalSection(ProjectConfigurationPlatforms) = postSolution
                		{B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                		{B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2}.Debug|Any CPU.Build.0 = Debug|Any CPU
                	EndGlobalSection
                EndGlobal
                """);

            var root = SolutionParser.Load(slnPath, out var err);
            Assert.Null(err);

            var proj = root!.Children.FirstOrDefault(c => c.Title.Equals("Dep.csproj", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(proj);

            var anchor = FindFileByTitle(proj!, "Anchor.cs");
            Assert.NotNull(anchor);
            Assert.Contains(anchor!.Children, c => c.Title.Equals("Other.cs", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    private static SolutionItem? FindFileByTitle(SolutionItem node, string title)
    {
        foreach (var c in node.Children)
        {
            if (c.FullPath is not null && c.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                return c;
            var inner = FindFileByTitle(c, title);
            if (inner is not null)
                return inner;
        }

        return null;
    }
}

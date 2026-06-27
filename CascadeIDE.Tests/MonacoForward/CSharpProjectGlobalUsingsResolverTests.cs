using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CSharpProjectGlobalUsingsResolverTests
{
    [Fact]
    public void Resolve_uses_sdk_implicit_usings_from_csproj()
    {
        var csproj = FindRepoFile(@"CasaField.Core\CasaField.Core.csproj");
        var source = Path.Combine(Path.GetDirectoryName(csproj)!, "Sample.cs");
        var text = CSharpProjectGlobalUsingsResolver.ResolveGlobalUsingsTree(source).ToString();

        Assert.Contains("global using System;", text);
        Assert.Contains("global using System.Linq;", text);
        Assert.DoesNotContain("global using System.Text;", text);
    }

    [Fact]
    public void Profile_merges_using_include_and_remove()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-usings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var csproj = Path.Combine(dir, "App.csproj");
            File.WriteAllText(csproj, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <Using Include="System.Text" />
                    <Using Remove="System.Net.Http" />
                  </ItemGroup>
                </Project>
                """);

            var profile = CSharpProjectUsingsProfile.TryLoad(csproj);
            Assert.NotNull(profile);
            var ns = profile!.ResolveNamespaces();
            Assert.Contains("System.Text", ns);
            Assert.Contains("System.Linq", ns);
            Assert.DoesNotContain("System.Net.Http", ns);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Resolve_prefers_generated_GlobalUsings_g_cs()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-usings-" + Guid.NewGuid().ToString("N"));
        var objDir = Path.Combine(dir, "obj", "Debug", "net10.0");
        Directory.CreateDirectory(objDir);
        try
        {
            var csproj = Path.Combine(dir, "App.csproj");
            File.WriteAllText(csproj, """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>""");
            var generated = Path.Combine(objDir, "GlobalUsings.g.cs");
            File.WriteAllText(generated, "global using System.Text;\n");

            var source = Path.Combine(dir, "Program.cs");
            File.WriteAllText(source, "class Program { }");

            var text = CSharpProjectGlobalUsingsResolver.ResolveGlobalUsingsTree(source).ToString();
            Assert.Contains("global using System.Text;", text);
            Assert.DoesNotContain("global using System.Linq;", text);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void FindOwningProjectFile_picks_nested_csproj()
    {
        var csproj = FindRepoFile(@"CasaField.Core\CasaField.Core.csproj");
        var source = Path.Combine(Path.GetDirectoryName(csproj)!, "Grid", "Decoder.cs");
        var found = CSharpProjectGlobalUsingsResolver.FindOwningProjectFile(source);
        Assert.Equal(csproj, found);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(dir, relativePath));
            if (File.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }

        throw new FileNotFoundException(relativePath);
    }
}

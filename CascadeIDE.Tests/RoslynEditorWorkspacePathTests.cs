using CascadeIDE.Services.Roslyn;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class RoslynEditorWorkspacePathTests
{
    [Fact]
    public void Resolve_SlnPath_ReturnsSln()
    {
        var path = RoslynEditorWorkspacePath.Resolve(@"C:\repo\app.sln", @"C:\repo\src\Foo.cs");
        Assert.Equal(@"C:\repo\app.sln", path);
    }

    [Fact]
    public void Resolve_SlnxPath_FindsCsprojNearFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-roslyn-path-" + Guid.NewGuid().ToString("N"));
        var srcDir = Path.Combine(root, "src");
        Directory.CreateDirectory(srcDir);
        var csproj = Path.Combine(srcDir, "App.csproj");
        File.WriteAllText(csproj, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var cs = Path.Combine(srcDir, "Foo.cs");
        File.WriteAllText(cs, "class Foo {}");
        var slnx = Path.Combine(root, "App.slnx");
        File.WriteAllText(slnx, "<Solution><Project Path=\"src/App.csproj\" /></Solution>");

        try
        {
            var path = RoslynEditorWorkspacePath.Resolve(slnx, cs, root);
            Assert.Equal(csproj, path);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Resolve_FolderWorkspace_FindsNearestCsproj()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-roslyn-path-" + Guid.NewGuid().ToString("N"));
        var srcDir = Path.Combine(root, "src");
        Directory.CreateDirectory(srcDir);
        var csproj = Path.Combine(root, "App.csproj");
        File.WriteAllText(csproj, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var cs = Path.Combine(srcDir, "Foo.cs");
        File.WriteAllText(cs, "class Foo {}");

        try
        {
            var path = RoslynEditorWorkspacePath.Resolve(null, cs, root);
            Assert.Equal(csproj, path);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }
}

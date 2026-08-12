using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassSolutionExplorerGlanceTests
{
    [Fact]
    public void TryLoad_standalone_csproj_uses_SolutionParser()
    {
        var dir = Path.Combine(Path.GetTempPath(), "glass-ssot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var csproj = Path.Combine(dir, "CdpMcp.csproj");
            File.WriteAllText(csproj, "<Project Sdk=\"Microsoft.NET.Sdk\"/>");
            File.WriteAllText(Path.Combine(dir, "Program.cs"), "System.Console.WriteLine();");

            var root = GlassSolutionExplorerGlance.TryLoad(csproj, out var err);
            Assert.Null(err);
            Assert.NotNull(root);
            Assert.Equal(Path.GetFullPath(csproj), Path.GetFullPath(root!.FullPath!));
            Assert.NotEmpty(root.Children);
            Assert.Contains(root.Children, c => c.FullPath is { } p && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

            var body = GlassSolutionExplorerGlance.TryFormat(root, csproj);
            Assert.NotNull(body);
            Assert.Contains("SolutionParser", body);
            Assert.Contains("CdpMcp", body);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void SolutionParser_Load_rejects_empty()
    {
        Assert.Null(SolutionParser.Load("", out var err));
        Assert.Contains("пуст", err!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryLoad_folder_uses_FolderWorkspaceTreeBuilder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "glass-folder-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.cs"), "// a");
            var root = GlassSolutionExplorerGlance.TryLoad(dir, out var err);
            Assert.Null(err);
            Assert.NotNull(root);
            Assert.Equal(Path.GetFullPath(dir), Path.GetFullPath(root!.FullPath!));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}

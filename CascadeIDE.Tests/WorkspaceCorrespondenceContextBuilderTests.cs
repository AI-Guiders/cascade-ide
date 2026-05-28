using System.Text.Json;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceCorrespondenceContextBuilderTests
{
    [Fact]
    public void BuildJson_includes_forward_docs_and_layers()
    {
        var root = Path.Combine(Path.GetTempPath(), "crs-ctx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var cs = Path.Combine(root, "Features", "Nav", "Bar.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(cs)!);
            File.WriteAllText(cs, "//");

            var tomlPath = Path.Combine(root, ".cascade");
            Directory.CreateDirectory(tomlPath);
            File.WriteAllText(
                Path.Combine(tomlPath, "workspace.toml"),
                """
                [workspace.adr.map]
                "Features/Nav/" = ["docs/adr/0001-test.md"]

                [[workspace.features.feature]]
                id = "nav"
                title = "Nav"
                paths = ["Features/Nav/"]
                docs = ["docs/adr/0001-test.md"]
                """);

            var adr = Path.Combine(root, "docs", "adr", "0001-test.md");
            Directory.CreateDirectory(Path.GetDirectoryName(adr)!);
            File.WriteAllText(adr, "# Test");

            var json = WorkspaceCorrespondenceContextBuilder.BuildJson(root, cs);
            using var doc = JsonDocument.Parse(json);
            var rootEl = doc.RootElement;
            Assert.Equal("Features/Nav/Bar.cs", rootEl.GetProperty("file").GetString()?.Replace('\\', '/'));
            Assert.Contains("L1", rootEl.GetProperty("activeLayers").EnumerateArray().Select(e => e.GetString()));
            Assert.True(rootEl.GetProperty("forwardDocs").GetArrayLength() >= 1);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}

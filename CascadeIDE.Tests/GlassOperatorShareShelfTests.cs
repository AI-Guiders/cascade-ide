#nullable enable

using CascadeIDE.Features.Cdp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassOperatorShareShelfTests
{
    [Fact]
    public void TryPut_writes_habitat_and_project_LATEST()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-share-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var body = "human note for model " + Guid.NewGuid().ToString("N")[..8];
            var inbox = GlassOperatorShareShelf.TryPut(body, root, shareId: "abcd1234ef01");
            Assert.NotNull(inbox);

            var projectLatest = Path.Combine(root, ".cdp", "share", "LATEST.md");
            Assert.True(File.Exists(projectLatest));
            Assert.Equal(body, File.ReadAllText(projectLatest));

            var habitatLatest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                CdpHabitatPaths.FolderName,
                "share",
                "LATEST.md");
            Assert.True(File.Exists(habitatLatest));
            Assert.Equal(body, File.ReadAllText(habitatLatest));

            var meta = Path.Combine(root, ".cdp", "share", "LATEST.json");
            Assert.True(File.Exists(meta));
            var metaText = File.ReadAllText(meta);
            Assert.Contains("share/v1", metaText, StringComparison.Ordinal);
            Assert.Contains("glass_intercom", metaText, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TryPut_empty_returns_null()
    {
        Assert.Null(GlassOperatorShareShelf.TryPut("   "));
    }
}

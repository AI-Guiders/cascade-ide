using System.IO;
using CascadeIDE.Features.Markdown.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ExpandedMarkdownFileExportProjectionTests
{
    [Fact]
    public void Plain_markdown_writes_expanded_file()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-md-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "src.md");
        var dst = Path.Combine(dir, "out.md");
        File.WriteAllText(src, "# Title\n");
        var r = ExpandedMarkdownFileExportProjection.TryExpandAndWriteAllText(src, "# Title\n", dst);
        Assert.True(r.Ok);
        Assert.Contains("# Title", File.ReadAllText(dst), StringComparison.Ordinal);
    }
}

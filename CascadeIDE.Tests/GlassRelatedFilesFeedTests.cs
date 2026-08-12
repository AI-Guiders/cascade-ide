#nullable enable

using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassRelatedFilesFeedTests
{
    [Fact]
    public void Collect_wnm_shape_same_stem_and_docs()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-rf-feed-" + Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(Path.Combine(root, "docs", "adr"));
        var editor = Path.Combine(src, "Foo.cs");
        File.WriteAllText(editor, "//");
        File.WriteAllText(Path.Combine(src, "Foo.xaml"), "<Grid/>");
        File.WriteAllText(Path.Combine(src, "Foo.md"), "#");
        File.WriteAllText(Path.Combine(root, "docs", "adr", "0001.md"), "#");

        try
        {
            var items = GlassRelatedFilesFeed.Collect(root, editor);
            Assert.Contains(items, i => i.Kind == "xaml_pair" && i.FullPath.EndsWith("Foo.xaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, i => i.Kind == "doc" && i.RelativePath.Contains("Foo.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, i => i.Kind == "workspace" && i.FullPath.Contains("0001.md", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}

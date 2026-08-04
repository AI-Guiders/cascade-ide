#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEditorSituRibbonTests
{
    [Fact]
    public void Format_why_and_blast_from_companions()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-editor-situ-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        try
        {
            var a = Path.Combine(root, "Foo.cs");
            var b = Path.Combine(root, "Foo.xaml");
            File.WriteAllText(a, "// a");
            File.WriteAllText(b, "<Grid/>");

            var line = GlassEditorSituRibbon.Format(
                a,
                root,
                why: "Glass Done (human flight)",
                leaf: "WHY-file + blast ribbon",
                blastMax: 3);

            Assert.Contains("WHY ·", line, StringComparison.Ordinal);
            Assert.Contains("Glass Done", line, StringComparison.Ordinal);
            Assert.Contains("BLAST ·", line, StringComparison.Ordinal);
            Assert.Contains("Foo.xaml", line, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Format_empty_without_editor()
    {
        Assert.Equal(string.Empty, GlassEditorSituRibbon.Format(null, null, "why", "leaf"));
    }
}

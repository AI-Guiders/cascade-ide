#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEditorSituRibbonTests
{
    [Fact]
    public void Build_face_splits_why_and_blast()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-editor-situ-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        try
        {
            var a = Path.Combine(root, "Foo.cs");
            var b = Path.Combine(root, "Foo.xaml");
            File.WriteAllText(a, "// a");
            File.WriteAllText(b, "<Grid/>");

            var face = GlassEditorSituRibbon.Build(
                a,
                root,
                why: "Glass Done (human flight)",
                leaf: "WHY-file + blast ribbon",
                blastMax: 3);

            Assert.True(face.HasAny);
            Assert.Contains("Glass Done", face.Why, StringComparison.Ordinal);
            Assert.Contains("Foo.xaml", face.Blast, StringComparison.Ordinal);
            Assert.Contains("Foo.xaml", face.BlastNames);
            Assert.False(face.Orphan);
            Assert.True(face.HopNodes >= 1);
            Assert.Contains("IN-MAP ·", face.RoleInGraph, StringComparison.Ordinal);
            Assert.Contains("map on MFD", face.RoleInGraph, StringComparison.Ordinal);
            // ROLE must not twin BLAST companion names
            Assert.DoesNotContain("Foo.xaml", face.RoleInGraph, StringComparison.Ordinal);

            var line = GlassEditorSituRibbon.Format(
                a,
                root,
                why: "Glass Done (human flight)",
                leaf: "WHY-file + blast ribbon",
                blastMax: 3);

            Assert.Contains("WHY ·", line, StringComparison.Ordinal);
            Assert.Contains("BLAST ·", line, StringComparison.Ordinal);
            Assert.Contains("ROLE ·", line, StringComparison.Ordinal);
            Assert.DoesNotContain("h1:", line, StringComparison.Ordinal);
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

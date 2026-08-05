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
            Assert.Equal("в карте", face.RoleInGraph);
            Assert.Equal("карта → MFD", face.LookMap);
            Assert.Contains("узлов", face.HopLine, StringComparison.Ordinal);
            Assert.DoesNotContain("IN-MAP", face.RoleInGraph, StringComparison.Ordinal);
            Assert.DoesNotContain("map on MFD", face.RoleInGraph, StringComparison.Ordinal);
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
            Assert.Contains("HOPS ·", line, StringComparison.Ordinal);
            Assert.Contains("LOOK ·", line, StringComparison.Ordinal);
            Assert.DoesNotContain("h1:", line, StringComparison.Ordinal);
            Assert.Contains("DIFF ·", line, StringComparison.Ordinal);
            Assert.Contains("APPLIES ·", line, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Build_applies_includes_scoped_test_fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-applies-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "Target.cs");
            File.WriteAllText(path, "class Target { }");
            var fails = new[]
            {
                new GlassTestOutputParse.FailRow("✗ TargetTests.Fail", "TargetTests.Fail", "boom"),
            };

            var face = GlassEditorSituRibbon.Build(path, root, why: null, leaf: null, testFails: fails);
            Assert.Contains("T1", face.AppliesOnLocus, StringComparison.Ordinal);
            Assert.Equal(1, face.Applies!.TestFails);
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

    [Fact]
    public void Build_applies_merges_scoped_build_problems()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-applies-wire-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "Broken.cs");
            File.WriteAllText(path, "class Broken { }");
            var problems = new[]
            {
                new GlassProblemItem(path, 1, 1, "error", "CS0001", "boom"),
                new GlassProblemItem(Path.Combine(root, "Other.cs"), 2, 1, "error", "CS0002", "other"),
            };

            var face = GlassEditorSituRibbon.Build(
                path,
                root,
                why: null,
                leaf: null,
                buildProblems: problems);

            Assert.Contains("E1", face.AppliesOnLocus, StringComparison.Ordinal);
            Assert.NotNull(face.Applies);
            Assert.Equal(1, face.Applies!.Errors);
            Assert.Contains(1, face.Applies.ErrorLines);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}

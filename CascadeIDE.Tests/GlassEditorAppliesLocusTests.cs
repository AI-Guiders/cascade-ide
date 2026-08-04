#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEditorAppliesLocusTests
{
    [Fact]
    public void Summarize_clean_when_no_problems()
    {
        var face = GlassEditorAppliesLocus.Summarize([]);
        Assert.True(face.Clean);
        Assert.Contains("CLEAN", face.Line, StringComparison.Ordinal);
        Assert.Contains("problems on MFD", face.Line, StringComparison.Ordinal);
        Assert.False(face.HasTint);
    }

    [Fact]
    public void Summarize_counts_and_error_lines()
    {
        var rows = new[]
        {
            new GlassProblemItem(@"C:\ws\Foo.cs", 12, 1, "error", "CS1002", "; expected"),
            new GlassProblemItem(@"C:\ws\Foo.cs", 12, 5, "error", "CS1513", "}" ),
            new GlassProblemItem(@"C:\ws\Foo.cs", 40, 1, "warning", "CS0168", "unused"),
        };

        var face = GlassEditorAppliesLocus.Summarize(rows, testFails: 1);
        Assert.False(face.Clean);
        Assert.Equal(2, face.Errors);
        Assert.Equal(1, face.Warnings);
        Assert.Equal(1, face.TestFails);
        Assert.Contains(12, face.ErrorLines);
        Assert.Contains(40, face.WarnLines);
        Assert.Contains("E2 W1", face.Line, StringComparison.Ordinal);
        Assert.Contains("T1", face.Line, StringComparison.Ordinal);
        Assert.True(face.HasTint);
    }

    [Fact]
    public void Collect_roslyn_syntax_error_on_cs()
    {
        var path = Path.Combine(Path.GetTempPath(), "applies-locus-" + Guid.NewGuid().ToString("N")[..8] + ".cs");
        try
        {
            File.WriteAllText(path, "class {");
            var face = GlassEditorAppliesLocus.Collect(path);
            Assert.False(face.Clean);
            Assert.True(face.Errors > 0);
            Assert.Contains("E", face.Line, StringComparison.Ordinal);
            Assert.True(face.HasTint);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}

#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEditorDiffIntentTests
{
    [Fact]
    public void Parse_maps_new_side_add_lines_and_summary()
    {
        const string diff = """
            diff --git a/Foo.cs b/Foo.cs
            --- a/Foo.cs
            +++ b/Foo.cs
            @@ -10,3 +10,5 @@
             context
            -old
            +new1
            +new2
             more
            """;

        var face = GlassEditorDiffIntent.Parse(diff);
        Assert.Equal(2, face.Added);
        Assert.Equal(1, face.Deleted);
        Assert.Equal(1, face.Hunks);
        Assert.Contains(11, face.AddLines);
        Assert.Contains(12, face.AddLines);
        Assert.Contains("+2 −1 · 1h", face.Line);
        Assert.False(face.Clean);
        Assert.True(face.HasTint);
    }

    [Fact]
    public void Parse_empty_is_clean_counts()
    {
        var face = GlassEditorDiffIntent.Parse("");
        Assert.Equal(0, face.Added);
        Assert.Equal(0, face.Hunks);
        Assert.True(face.Clean);
        Assert.False(face.HasTint);
    }
}

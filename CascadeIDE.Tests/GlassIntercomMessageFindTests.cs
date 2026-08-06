#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassIntercomMessageFindTests
{
    [Fact]
    public void TryParseNeedle_bracket_path_line()
    {
        Assert.True(GlassIntercomMessageFind.TryParseNeedle("[src/Foo.cs:10-12]", out var needle, out _));
        Assert.Equal("src/Foo.cs", needle.File.Replace('\\', '/'));
        Assert.Equal(10, needle.LineStart);
        Assert.Equal(12, needle.LineEnd);
    }

    [Fact]
    public void TryParseNeedle_bare_path()
    {
        Assert.True(GlassIntercomMessageFind.TryParseNeedle("Bar.cs:3", out var needle, out _));
        Assert.Equal("Bar.cs", Path.GetFileName(needle.File));
        Assert.Equal(3, needle.LineStart);
    }

    [Fact]
    public void MatchOrdinals_suffix_file_and_line_overlap()
    {
        var needle = new GlassAttachChip("n", "Foo.cs", 10, 12);
        var feed = new[]
        {
            new GlassIntercomMessageFind.Hit(1, "no chip", null),
            new GlassIntercomMessageFind.Hit(2, "see [src/Foo.cs:11]", GlassAttachChipPeel.FromBody("see [src/Foo.cs:11]")),
            new GlassIntercomMessageFind.Hit(3, "other [Bar.cs:1]", GlassAttachChipPeel.FromBody("other [Bar.cs:1]")),
        };

        var hits = GlassIntercomMessageFind.MatchOrdinals(needle, feed);
        Assert.Equal(new[] { 2 }, hits.ToArray());
    }

    [Fact]
    public void ApplyOrdinals_selects_hits()
    {
        var apply = GlassIntercomMessageSelect.ApplyOrdinals(5, [2, 4], out var sel);
        Assert.Equal("OK", apply);
        Assert.Equal(4, sel.ActiveOrdinal);
        Assert.True(sel.Highlighted.SetEquals([2, 4]));
    }
}

using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapHciOrientationTests
{
    [Theory]
    [InlineData(@"D:\repo\src\Foo.cs", "Foo")]
    [InlineData("/x/y/Bar.g.cs", "Bar.g")]
    [InlineData("NoExt", "NoExt")]
    public void BuildQueryFromCurrentPath_returns_file_stem_or_name(string path, string expected)
    {
        var q = SemanticMapHciOrientationAcquirer.BuildQueryFromCurrentPath(path);
        Assert.Equal(expected, q);
    }

    [Fact]
    public void ToStatusLine_empty_when_no_hits_and_no_error()
    {
        var s = new SemanticMapHciOrientationSnapshot([], "Foo", null);
        Assert.Equal("", SemanticMapHciOrientationFormatting.ToStatusLine(s));
    }

    [Fact]
    public void ToStatusLine_shows_error()
    {
        var s = new SemanticMapHciOrientationSnapshot([], "x", "index missing");
        Assert.Equal("HCI (ориентация): index missing", SemanticMapHciOrientationFormatting.ToStatusLine(s));
    }

    [Fact]
    public void ToStatusLine_one_hit()
    {
        var s = new SemanticMapHciOrientationSnapshot(
            [new SemanticMapHciOrientationHit("A.cs", "text_fts", 3, "hello")],
            "A",
            null);
        var line = SemanticMapHciOrientationFormatting.ToStatusLine(s);
        Assert.Contains("HCI (ориентация) «A»", line);
        Assert.Contains("A.cs:3 (text_fts)", line);
        Assert.Contains("hello", line);
    }

    [Fact]
    public void ToStatusLine_truncates_snippet()
    {
        var longSn = new string('a', 80);
        var s = new SemanticMapHciOrientationSnapshot(
            [new SemanticMapHciOrientationHit("B.cs", "text_fts", 1, longSn)],
            "B",
            null);
        var line = SemanticMapHciOrientationFormatting.ToStatusLine(s);
        Assert.Contains("…", line);
        Assert.DoesNotContain(longSn, line);
    }
}

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassTopicCardSummaryTests
{
    [Fact]
    public void Format_MetaOnly_WhenBodiesEmpty()
    {
        var start = new DateTimeOffset(2026, 8, 5, 5, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(10);
        var s = GlassTopicCardSummary.Format(2, start, end, []);
        Assert.Contains("2 msg", s);
        Assert.DoesNotContain('\n', s);
    }

    [Fact]
    public void Format_AppendsTruncatedLastBody()
    {
        var start = new DateTimeOffset(2026, 8, 5, 5, 0, 0, TimeSpan.Zero);
        var s = GlassTopicCardSummary.Format(1, start, start, ["first", "last line here"]);
        Assert.Contains('\n', s);
        Assert.EndsWith("last line here", s);
    }

    [Fact]
    public void Truncate_CapsAtMax()
    {
        var longBody = new string('x', GlassTopicCardSummary.MaxBodyLen + 40);
        var t = GlassTopicCardSummary.Truncate(longBody);
        Assert.EndsWith("…", t);
        Assert.True(t.Length <= GlassTopicCardSummary.MaxBodyLen + 1);
    }
}

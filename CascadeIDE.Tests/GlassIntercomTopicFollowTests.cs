#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomTopicFollowTests
{
    static GlassIntercomTopics.Topic T(string id, params string[] entryIds) =>
        new(id, id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, entryIds.Length, entryIds);

    [Fact]
    public void All_stays_all()
    {
        var topics = new[] { T("a", "a1"), T("b", "b1") };
        Assert.Null(GlassIntercomTopicFollow.AfterStickEnd(null, topics, "b1"));
    }

    [Fact]
    public void Same_topic_keeps_selection()
    {
        var topics = new[] { T("a", "a1", "a2"), T("b", "b1") };
        Assert.Equal("a", GlassIntercomTopicFollow.AfterStickEnd("a", topics, "a2"));
    }

    [Fact]
    public void Gap_new_topic_follows_newest()
    {
        var topics = new[] { T("a", "a1"), T("b", "b1") };
        Assert.Equal("b", GlassIntercomTopicFollow.AfterStickEnd("a", topics, "b1"));
    }

    [Fact]
    public void Ordinal_1_based()
    {
        var topics = new[] { T("a", "a1"), T("b", "b1") };
        Assert.Equal("b", GlassIntercomTopicFollow.IdByOrdinal(topics, 2));
        Assert.Null(GlassIntercomTopicFollow.IdByOrdinal(topics, 0));
        Assert.Null(GlassIntercomTopicFollow.IdByOrdinal(topics, 3));
    }
}

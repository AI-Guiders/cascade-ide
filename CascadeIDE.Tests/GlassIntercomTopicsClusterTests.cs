#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomTopicsClusterTests
{
    static GlassIntercomTopics.Stamp S(string id, string body, DateTimeOffset utc) =>
        new(id, body, utc);

    [Fact]
    public void Empty_returns_no_topics()
    {
        Assert.Empty(GlassIntercomTopics.Cluster([]));
    }

    [Fact]
    public void Single_entry_one_topic()
    {
        var t0 = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var topics = GlassIntercomTopics.Cluster([S("a", "hello\nrest", t0)]);
        Assert.Single(topics);
        Assert.Equal("a", topics[0].Id);
        Assert.Equal(1, topics[0].Count);
        Assert.Contains("hello", topics[0].Title);
        Assert.Equal(["a"], topics[0].EntryIds);
    }

    [Fact]
    public void Gap_over_default_splits_topics()
    {
        var t0 = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var t1 = t0.AddMinutes(10);
        var t2 = t0.AddMinutes(50); // > 30m from t1
        var topics = GlassIntercomTopics.Cluster(
        [
            S("a", "first", t0),
            S("b", "same bucket", t1),
            S("c", "new topic", t2),
        ]);
        Assert.Equal(2, topics.Count);
        Assert.Equal(2, topics[0].Count);
        Assert.Equal(["a", "b"], topics[0].EntryIds);
        Assert.Equal(1, topics[1].Count);
        Assert.Equal(["c"], topics[1].EntryIds);
    }

    [Fact]
    public void Unsorted_input_orders_by_stamp()
    {
        var t0 = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var topics = GlassIntercomTopics.Cluster(
        [
            S("late", "second", t0.AddMinutes(5)),
            S("early", "first", t0),
        ]);
        Assert.Single(topics);
        Assert.Equal(["early", "late"], topics[0].EntryIds);
        Assert.Contains("first", topics[0].Title);
    }

    [Fact]
    public void Custom_gap_controls_split()
    {
        var t0 = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var topics = GlassIntercomTopics.Cluster(
        [
            S("a", "x", t0),
            S("b", "y", t0.AddMinutes(2)),
        ],
        gap: TimeSpan.FromMinutes(1));
        Assert.Equal(2, topics.Count);
    }

    [Fact]
    public void Blank_body_uses_topic_ordinal_title()
    {
        var t0 = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var topics = GlassIntercomTopics.Cluster([S("z", "   \n", t0)]);
        Assert.Single(topics);
        Assert.Contains("topic 1", topics[0].Title);
    }

    [Fact]
    public void Non_positive_gap_falls_back_to_default()
    {
        var t0 = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var topics = GlassIntercomTopics.Cluster(
        [
            S("a", "x", t0),
            S("b", "y", t0.AddMinutes(10)),
        ],
        gap: TimeSpan.Zero);
        Assert.Single(topics);
    }
}

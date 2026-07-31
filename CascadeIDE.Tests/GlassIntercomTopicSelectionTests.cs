#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomTopicSelectionTests
{
    static GlassIntercomTopics.Topic T(string id, params string[] entryIds) =>
        new(id, id, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, entryIds.Length, entryIds);

    [Fact]
    public void Exact_id_wins()
    {
        var topics = new[] { T("a", "a", "b"), T("c", "c") };
        Assert.Equal("c", GlassIntercomTopicSelection.Survive("c", topics, ["a"]));
    }

    [Fact]
    public void Rematch_by_prior_entry_ids_when_first_entry_aged_out()
    {
        // Old topic id was "old-first"; LoadTail dropped it — bucket now starts at "keep".
        var topics = new[] { T("keep", "keep", "later"), T("other", "other") };
        var survived = GlassIntercomTopicSelection.Survive(
            "old-first",
            topics,
            priorEntryIds: ["old-first", "keep", "later"]);
        Assert.Equal("keep", survived);
    }

    [Fact]
    public void No_overlap_clears_to_all()
    {
        var topics = new[] { T("x", "x") };
        Assert.Null(GlassIntercomTopicSelection.Survive("gone", topics, ["zzz"]));
    }

    [Fact]
    public void Empty_topics_clears()
    {
        Assert.Null(GlassIntercomTopicSelection.Survive("a", [], ["a"]));
    }
}

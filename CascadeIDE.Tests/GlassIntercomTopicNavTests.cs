#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomTopicNavTests
{
    static readonly string[] Ids = ["a", "b", "c"];

    [Fact]
    public void Next_from_all_picks_first()
    {
        Assert.Equal("a", GlassIntercomTopicNav.Next(null, Ids));
        Assert.Equal("a", GlassIntercomTopicNav.Next("", Ids));
    }

    [Fact]
    public void Next_advances_and_clamps()
    {
        Assert.Equal("b", GlassIntercomTopicNav.Next("a", Ids));
        Assert.Equal("c", GlassIntercomTopicNav.Next("b", Ids));
        Assert.Equal("c", GlassIntercomTopicNav.Next("c", Ids));
    }

    [Fact]
    public void Prev_from_all_picks_last()
    {
        Assert.Equal("c", GlassIntercomTopicNav.Prev(null, Ids));
    }

    [Fact]
    public void Prev_retreats_and_clamps()
    {
        Assert.Equal("b", GlassIntercomTopicNav.Prev("c", Ids));
        Assert.Equal("a", GlassIntercomTopicNav.Prev("b", Ids));
        Assert.Equal("a", GlassIntercomTopicNav.Prev("a", Ids));
    }

    [Fact]
    public void Unknown_id_resets_to_edge()
    {
        Assert.Equal("a", GlassIntercomTopicNav.Next("missing", Ids));
        Assert.Equal("c", GlassIntercomTopicNav.Prev("missing", Ids));
    }

    [Fact]
    public void Empty_list_keeps_current()
    {
        Assert.Equal("x", GlassIntercomTopicNav.Next("x", []));
        Assert.Null(GlassIntercomTopicNav.Prev(null, []));
    }
}

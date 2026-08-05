#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomChannelTests
{
    [Fact]
    public void Parse_codes_and_aliases()
    {
        Assert.Equal(GlassIntercomChannel.Kind.Crew, GlassIntercomChannel.Parse("crew"));
        Assert.Equal(GlassIntercomChannel.Kind.Crew, GlassIntercomChannel.Parse("#crew"));
        Assert.Equal(GlassIntercomChannel.Kind.Radio, GlassIntercomChannel.Parse("radio"));
        Assert.Equal(GlassIntercomChannel.Kind.Dm, GlassIntercomChannel.Parse("dm"));
        Assert.Equal(GlassIntercomChannel.Kind.Dm, GlassIntercomChannel.Parse("1:1"));
        Assert.Equal(GlassIntercomChannel.DefaultKind, GlassIntercomChannel.Parse(""));
    }

    [Fact]
    public void Labels_match_northstar_face()
    {
        Assert.Equal("#crew", GlassIntercomChannel.Label(GlassIntercomChannel.Kind.Crew));
        Assert.Equal("Radio", GlassIntercomChannel.Label(GlassIntercomChannel.Kind.Radio));
        Assert.Equal("DM", GlassIntercomChannel.Label(GlassIntercomChannel.Kind.Dm));
    }

    [Fact]
    public void FormatLatchJson_roundtrips()
    {
        var json = GlassIntercomChannel.FormatLatchJson(
            GlassIntercomChannel.Kind.Crew,
            DateTimeOffset.Parse("2026-08-05T00:00:00Z"));
        var snap = GlassIntercomChannel.ParseLatchJson(json);
        Assert.Equal(GlassIntercomChannel.Kind.Crew, snap.Channel);
    }

    [Fact]
    public void ParseLatchJson_default_when_empty()
    {
        Assert.Equal(GlassIntercomChannel.Kind.Radio, GlassIntercomChannel.ParseLatchJson(null).Channel);
        Assert.Equal(GlassIntercomChannel.Kind.Radio, GlassIntercomChannel.ParseLatchJson("{}").Channel);
    }
}

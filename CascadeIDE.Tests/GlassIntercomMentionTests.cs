#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomMentionTests
{
    static GlassIntercomMention.MentionRoster LiveRoster => new(
        PfName: "Sierra",
        PfKind: "citizen",
        PmName: "Света",
        PmKind: "operator");

    [Theory]
    [InlineData("@PF", true)]
    [InlineData("@pf hello", true)]
    [InlineData("hey @PF please", true)]
    [InlineData("hello", false)]
    [InlineData("@PFOO", false)]
    [InlineData("Message @PF…", false)]
    public void MentionsPf_word_boundary(string? body, bool expect) =>
        Assert.Equal(expect, GlassIntercomMention.MentionsPf(body));

    [Theory]
    [InlineData("@PM", true)]
    [InlineData("@PM это я", true)]
    [InlineData("@PMOO", false)]
    [InlineData("Message @PM…", false)]
    public void MentionsPm_word_boundary(string? body, bool expect) =>
        Assert.Equal(expect, GlassIntercomMention.MentionsPm(body));

    [Theory]
    [InlineData("@guest", true)]
    [InlineData("ping @citizen", true)]
    [InlineData("@operator please", true)]
    [InlineData("@guests", false)]
    public void MentionsKind_tags(string body, bool expect)
    {
        var any = GlassIntercomMention.MentionsKind(body, "guest")
            || GlassIntercomMention.MentionsKind(body, "citizen")
            || GlassIntercomMention.MentionsKind(body, "operator");
        Assert.Equal(expect, any);
    }

    [Fact]
    public void ResolveWakes_PF_citizen_goes_habitat_not_cannon()
    {
        var hits = GlassIntercomMention.ResolveWakes("@PF look", LiveRoster);
        Assert.Contains(hits, h => h.Sink == GlassIntercomMention.WakeSink.HabitatCitizen);
        Assert.DoesNotContain(hits, h => h.Sink == GlassIntercomMention.WakeSink.ExternalGuest);
    }

    [Fact]
    public void ResolveWakes_guest_kind_fires_external()
    {
        var hits = GlassIntercomMention.ResolveWakes("hey @guest", LiveRoster);
        Assert.Contains(hits, h => h.Sink == GlassIntercomMention.WakeSink.ExternalGuest);
    }

    [Fact]
    public void ResolveWakes_Who_Sierra_and_Sveta()
    {
        var hits = GlassIntercomMention.ResolveWakes("@Sierra и @Света", LiveRoster);
        Assert.Contains(hits, h => h.Sink == GlassIntercomMention.WakeSink.HabitatCitizen);
        Assert.Contains(hits, h => h.Sink == GlassIntercomMention.WakeSink.GlassOperator);
    }

    [Fact]
    public void ResolveWakes_Kir_alias_external_even_if_PF_is_citizen()
    {
        var hits = GlassIntercomMention.ResolveWakes("@Kir призовись", LiveRoster);
        Assert.Contains(hits, h => h.Sink == GlassIntercomMention.WakeSink.ExternalGuest);
    }

    [Fact]
    public void ResolveWakes_dedupes_seat_and_Who()
    {
        var hits = GlassIntercomMention.ResolveWakes("@PF @Sierra", LiveRoster);
        Assert.Single(hits);
        Assert.Equal(GlassIntercomMention.WakeSink.HabitatCitizen, hits[0].Sink);
    }

    [Fact]
    public void FormatWakeNote_shows_Who_when_known()
    {
        Assert.Equal("@PF→Sierra wake", GlassIntercomMention.FormatWakeNote(GlassIntercomMention.Seat.Pf, "Sierra"));
        Assert.Equal("@PM→Света wake", GlassIntercomMention.FormatWakeNote(GlassIntercomMention.Seat.Pm, "Света"));
    }

    [Fact]
    public void TryGetAtToken_and_Suggest_filter_by_prefix()
    {
        Assert.True(GlassIntercomMention.TryGetAtToken("hey @Ki", 7, out var start, out var prefix));
        Assert.Equal(4, start);
        Assert.Equal("Ki", prefix);

        var hits = GlassIntercomMention.Suggest("Ki", LiveRoster);
        Assert.Contains(hits, h => h.Title.Equals("@Kir", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(hits, h => h.Title.Equals("@PM", StringComparison.OrdinalIgnoreCase));
    }

}

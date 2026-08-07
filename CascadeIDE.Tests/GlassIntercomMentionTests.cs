#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomMentionTests
{
    [Theory]
    [InlineData("@PF", true)]
    [InlineData("@pf hello", true)]
    [InlineData("hey @PF please", true)]
    [InlineData("@PF, Sierra", true)]
    [InlineData("hello", false)]
    [InlineData("@CIT", false)]
    [InlineData("@PFOO", false)]
    [InlineData("email@pf.com", false)]
    [InlineData("Message @PF…", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void MentionsPf_word_boundary(string? body, bool expect) =>
        Assert.Equal(expect, GlassIntercomMention.MentionsPf(body));

    [Theory]
    [InlineData("@PM", true)]
    [InlineData("@pm hello", true)]
    [InlineData("hey @PM please", true)]
    [InlineData("@PM это я", true)]
    [InlineData("@PF", false)]
    [InlineData("@PMOO", false)]
    [InlineData("Message @PM…", false)]
    [InlineData("hello", false)]
    public void MentionsPm_word_boundary(string? body, bool expect) =>
        Assert.Equal(expect, GlassIntercomMention.MentionsPm(body));

    [Fact]
    public void FormatWakeNote_shows_Who_when_known()
    {
        Assert.Equal("@PF→Sierra wake", GlassIntercomMention.FormatWakeNote(GlassIntercomMention.Seat.Pf, "Sierra"));
        Assert.Equal("@PM→Света wake", GlassIntercomMention.FormatWakeNote(GlassIntercomMention.Seat.Pm, "Света"));
        Assert.Equal("@PM wake", GlassIntercomMention.FormatWakeNote(GlassIntercomMention.Seat.Pm, null));
    }
}

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
}

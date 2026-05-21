#nullable enable

using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashLegacyIntercomTests
{
    [Theory]
    [InlineData("/topic list")]
    [InlineData("/spine show")]
    [InlineData("/overview")]
    [InlineData("/attach selection")]
    [InlineData("/card My")]
    [InlineData("/thread next")]
    public void Parse_LegacyTopLevelHead_IsRejected(string line)
    {
        var parse = ChatSlashCommandParser.TryParse(line);
        Assert.True(parse.IsSlashLine);
        Assert.True(parse.IsRejected);
        Assert.Contains("/intercom", parse.RejectReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_IntercomTopicList_IsAccepted()
    {
        var parse = ChatSlashCommandParser.TryParse("/intercom topic list");
        Assert.True(parse.IsSlashLine);
        Assert.False(parse.IsRejected);
        Assert.Equal("intercom", parse.Head);
    }
}

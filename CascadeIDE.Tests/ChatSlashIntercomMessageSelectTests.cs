using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashIntercomMessageSelectTests
{
    [Theory]
    [InlineData("/intercom message select 3", "3", "3")]
    [InlineData("/intercom message select 3 5", "3 5", "3")]
    [InlineData("/intercom message select 3:5", "3:5", "3")]
    public void Parse_StripsSelectVerb_LeavesLineRangeTail(string line, string expectedArgs, string expectedStart)
    {
        var parse = ChatSlashCommandParser.TryParse(line);
        Assert.True(parse.IsSlashLine);
        Assert.Equal(expectedArgs, parse.ArgsTail);
        Assert.True(ChatSlashParametricArgsBuilder.TryParseLineRangeTail(parse.ArgsTail, out var start, out _, out _));
        Assert.Equal(int.Parse(expectedStart), start);
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/intercom message select", d.SlashPath);
    }

    [Fact]
    public void TryParseLineRangeTail_RejectsThreeTokens()
    {
        Assert.False(ChatSlashParametricArgsBuilder.TryParseLineRangeTail("3 5 7", out _, out _, out var error));
        Assert.Contains("Ожидается", error);
    }
}

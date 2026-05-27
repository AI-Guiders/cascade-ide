using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashIntercomMessageSelectTests
{
    [Theory]
    [InlineData("/intercom message select 3", "3", "3")]
    [InlineData("/intercom message select 3 5", "3 5", "3")]
    [InlineData("/intercom message select 3:5", "3:5", "3")]
    public void ResolveInput_MessageSelect_ArgTail(string line, string expectedArgs, string expectedStart)
    {
        Assert.True(SlashLineResolver.TryResolveSlashLine(line, out var resolved));
        Assert.Equal("/intercom message select", resolved.CanonicalPath);
        Assert.Equal(expectedArgs, resolved.ArgTail);
        Assert.True(ChatSlashParametricArgsBuilder.TryParseLineRangeTail(resolved.ArgTail, out var start, out _, out _));
        Assert.Equal(int.Parse(expectedStart), start);
        ChatSlashCatalogTestSupport.AssertResolves(line, "/intercom message select");
    }

    [Fact]
    public void TryParseLineRangeTail_RejectsThreeTokens()
    {
        Assert.False(ChatSlashParametricArgsBuilder.TryParseLineRangeTail("3 5 7", out _, out _, out var error));
        Assert.Contains("Ожидается", error);
    }

    [Fact]
    public void ResolveInput_MessageSelectClear()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/intercom message select clear", "/intercom message select clear");
    }

    [Fact]
    public void ResolveInput_BracketSegments_InMessageSelect()
    {
        const string line = "/intercom message select [3;5] [8;15] [20]";
        Assert.True(SlashLineResolver.TryResolveSlashLine(line, out var resolved));
        Assert.Equal("[3;5] [8;15] [20]", resolved.ArgTail);
        Assert.True(ParametricSegmentListParser.TryParse(resolved.ArgTail, out var segments, out _));
        Assert.Equal(3, segments.Count);
        ChatSlashCatalogTestSupport.AssertResolves(line, "/intercom message select");
    }
}

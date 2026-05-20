using CascadeIDE.Features.Chat.Application;
using CascadeIDE.Features.IdeMcp.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomApplicationComputingUnitTests
{
    [Theory]
    [InlineData("⟦a:abcd1234⟧", true)]
    [InlineData("see [F:Foo.cs M:Run]", true)]
    [InlineData("plain text", false)]
    [InlineData("", false)]
    public void IntercomAttachSyntax_detects_wire_and_bracket(string text, bool expected) =>
        Assert.Equal(expected, IntercomAttachSyntax.HasWireOrBracketSyntax(text));

    [Theory]
    [InlineData("assistant", "hello", true)]
    [InlineData("user", "[F:a.cs]", true)]
    [InlineData("user", "no attach", false)]
    public void IntercomMcpSendChatRoute_routes_fast_append(string role, string message, bool expected) =>
        Assert.Equal(expected, IntercomMcpSendChatRoute.ShouldAppendPreparedFeedMessage(role, message));

    [Fact]
    public void IntercomOutboundPrepareProfile_mcp_fast_skips_roslyn_and_git()
    {
        var p = IntercomOutboundPrepareProfile.McpFastPrepare;
        Assert.True(p.SkipMemberRoslynAtSend);
        Assert.False(p.CaptureSenderWorkspaceContext);
        Assert.True(p.AddMcpFastPathWarning);
    }
}

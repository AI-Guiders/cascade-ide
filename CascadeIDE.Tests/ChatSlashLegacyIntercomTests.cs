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
    public void LegacyTopLevel_DoesNotResolve(string line)
    {
        ChatSlashCatalogTestSupport.AssertDoesNotResolve(line);
    }

    [Fact]
    public void IntercomTopicList_Resolves()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/intercom topic list", "/intercom topic list");
    }
}

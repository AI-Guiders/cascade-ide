#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashIntercomMessageFindTests
{
    [Theory]
    [InlineData("/intercom message find selection", "selection")]
    [InlineData("/intercom message find L:3-7", "L:3-7")]
    public void ResolveInput_MessageFind_ArgTail(string line, string expectedTail)
    {
        Assert.True(SlashLineResolver.TryResolveSlashLine(line, out var resolved));
        Assert.Equal("/intercom message find", resolved.CanonicalPath);
        Assert.Equal(expectedTail, resolved.ArgTail);
        ChatSlashCatalogTestSupport.AssertResolves(line, "/intercom message find", expectedTail);
    }

    [Fact]
    public void Catalog_ResolvesMessageFind()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/intercom message find selection", "/intercom message find", "selection");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput("/intercom message find selection", out var d, out _));
        Assert.True(IntentSlashCatalog.TryGetRoute(d.SlashPath, out var route));
        Assert.Equal("message_find", route.IntercomHandlerId);
    }
}

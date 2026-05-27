using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatTopicRenameTests
{
    [Fact]
    public void ResolveInput_topic_rename()
    {
        ChatSlashCatalogTestSupport.AssertResolves(
            "/intercom topic rename New title",
            "/intercom topic rename",
            "New title");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput("/intercom topic rename New title", out var descriptor, out _));
        Assert.True(IntentSlashCatalog.TryGetRoute(descriptor.SlashPath, out var route));
        Assert.Equal("topic_rename", route.IntercomHandlerId);
    }

    [Fact]
    public void Intercom_handler_topic_rename_is_registered()
    {
        Assert.True(ChatSlashIntercomHandlers.IsKnown("topic_rename"));
    }
}

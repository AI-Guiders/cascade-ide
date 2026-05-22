using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatTopicRenameTests
{
    [Fact]
    public void Parse_topic_rename_resolves_catalog_route()
    {
        var parse = ChatSlashCommandParser.TryParse("/intercom topic rename New title");
        Assert.True(parse.IsSlashLine);
        Assert.False(parse.IsRejected);
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var descriptor));
        Assert.Equal("/intercom topic rename", descriptor.SlashPath);
        Assert.True(IntentSlashCatalog.TryGetRoute(descriptor.SlashPath, out var route));
        Assert.Equal("topic_rename", route.IntercomHandlerId);
    }

    [Fact]
    public void Intercom_handler_topic_rename_is_registered()
    {
        Assert.True(ChatSlashIntercomHandlers.IsKnown("topic_rename"));
    }
}

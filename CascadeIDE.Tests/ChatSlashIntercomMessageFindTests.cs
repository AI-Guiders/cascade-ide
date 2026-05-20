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
    public void Parse_StripsFindVerb(string line, string expectedTail)
    {
        var parse = ChatSlashCommandParser.TryParse(line);
        Assert.True(parse.IsSlashLine);
        Assert.Equal("message", parse.Action);
        Assert.Equal(expectedTail, parse.ArgsTail);

        Assert.True(IntercomSlashPathBuilder.TryBuildPath(parse, out var path));
        Assert.Equal("/intercom message find", path);
    }

    [Fact]
    public void Catalog_ResolvesMessageFind()
    {
        var parse = ChatSlashCommandParser.TryParse("/intercom message find selection");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/intercom message find", d.SlashPath);
        Assert.True(IntentSlashCatalog.TryGetRoute(d.SlashPath, out var route));
        Assert.Equal("message_find", route.IntercomHandlerId);
    }
}

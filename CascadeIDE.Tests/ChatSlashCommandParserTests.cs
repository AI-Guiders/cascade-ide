using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashCommandParserTests
{
    [Fact]
    public void TryParse_NotSlash_ReturnsNotSlash()
    {
        var r = ChatSlashCommandParser.TryParse("hello");
        Assert.False(r.IsSlashLine);
    }

    [Fact]
    public void TryParse_FlatOverview()
    {
        var r = ChatSlashCommandParser.TryParse("/overview");
        Assert.True(r.IsSlashLine);
        Assert.False(r.IsRejected);
        Assert.Equal(ChatSlashCommandShape.Flat, r.Shape);
        Assert.Equal("overview", r.Head);
    }

    [Fact]
    public void TryParse_NamespaceAction_BuildRun()
    {
        var r = ChatSlashCommandParser.TryParse("/build run");
        Assert.True(r.IsSlashLine);
        Assert.Equal(ChatSlashCommandShape.NamespaceAction, r.Shape);
        Assert.Equal("build", r.Head);
        Assert.Equal("run", r.Action);
    }

    [Fact]
    public void TryParse_CardWithArgsTail()
    {
        var r = ChatSlashCommandParser.TryParse("/card ADR 0119");
        Assert.True(r.IsSlashLine);
        Assert.Equal(ChatSlashCommandShape.Flat, r.Shape);
        Assert.Equal("card", r.Head);
        Assert.Equal("ADR 0119", r.ArgsTail);
    }

    [Theory]
    [InlineData("/foo")]
    [InlineData("/")]
    public void TryParse_UnknownOrEmpty_RejectedOrResolvable(string line)
    {
        var r = ChatSlashCommandParser.TryParse(line);
        Assert.True(r.IsSlashLine);
    }

    [Fact]
    public void Catalog_ResolvesBuildRun()
    {
        var parse = ChatSlashCommandParser.TryParse("/build run");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/build run", d.SlashPath);
        Assert.Equal(Services.IdeCommands.Build, d.CommandId);
    }

    [Fact]
    public void Catalog_Help_IsLocal()
    {
        var parse = ChatSlashCommandParser.TryParse("/help");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal(ChatSlashCommandExecutionKind.LocalHelp, d.ExecutionKind);
    }
}

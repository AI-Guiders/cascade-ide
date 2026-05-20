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
    public void TryParse_IntercomOverview()
    {
        var r = ChatSlashCommandParser.TryParse("/intercom overview");
        Assert.True(r.IsSlashLine);
        Assert.False(r.IsRejected);
        Assert.Equal(ChatSlashCommandShape.NamespaceAction, r.Shape);
        Assert.Equal("intercom", r.Head);
        Assert.Equal("overview", r.Action);
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
    public void TryParse_IntercomTopicCreateWithoutTitle_EmptyArgsTail()
    {
        var r = ChatSlashCommandParser.TryParse("/intercom topic create");
        Assert.True(r.IsSlashLine);
        Assert.Equal("", r.ArgsTail);
        Assert.True(ChatSlashCommandCatalog.TryResolve(r, out var d));
        Assert.Equal("/intercom topic create", d.SlashPath);
    }

    [Fact]
    public void TryParse_IntercomTopicCreateWithArgsTail()
    {
        var r = ChatSlashCommandParser.TryParse("/intercom topic create ADR 0119");
        Assert.True(r.IsSlashLine);
        Assert.Equal(ChatSlashCommandShape.NamespaceAction, r.Shape);
        Assert.Equal("intercom", r.Head);
        Assert.Equal("topic", r.Action);
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

    [Theory]
    [InlineData("/file open src/Foo.cs", true)]
    [InlineData("/solution load My.sln", true)]
    [InlineData("/file open", false)]
    [InlineData("/build run", false)]
    public void ShouldAutoExecuteAfterAutocompleteCommit(string line, bool expected) =>
        Assert.Equal(expected, ChatSlashCommandParser.ShouldAutoExecuteAfterAutocompleteCommit(line));
}

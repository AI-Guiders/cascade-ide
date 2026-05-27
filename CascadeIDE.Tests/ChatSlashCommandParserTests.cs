using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashCommandParserTests
{
    [Fact]
    public void IsSlashLine_NotSlash_ReturnsFalse()
    {
        Assert.False(ChatSlashCommandParser.IsSlashLine("hello"));
    }

    [Fact]
    public void TryResolveInput_IntercomOverview()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/intercom overview", "/intercom overview");
    }

    [Fact]
    public void TryResolveInput_BuildRun()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/build run", "/build run");
    }

    [Fact]
    public void TryResolveInput_IntercomTopicCreate()
    {
        ChatSlashCatalogTestSupport.AssertResolves(
            "/intercom topic create",
            "/intercom topic create");
    }

    [Fact]
    public void TryResolveInput_IntercomTopicCreate_WithTitle()
    {
        ChatSlashCatalogTestSupport.AssertResolves(
            "/intercom topic create ADR 0119",
            "/intercom topic create",
            "ADR 0119");
    }

    [Theory]
    [InlineData("/intercom server status", "/intercom server status")]
    [InlineData("/build run", "/build run")]
    [InlineData("/help", "/help")]
    public void TryResolveInput_CatalogPaths(string line, string expectedPath)
    {
        ChatSlashCatalogTestSupport.AssertResolves(line, expectedPath);
    }

    [Theory]
    [InlineData("/intercom topic open bbbbbbbb", true)]
    [InlineData("/intercom topic open Ветка", true)]
    [InlineData("/intercom spine open", true)]
    [InlineData("/intercom topic cards", true)]
    [InlineData("/intercom topic open", true)]
    public void ShouldAutoExecuteAfterAutocompleteCommit(string line, bool expected)
    {
        Assert.Equal(expected, ChatSlashCommandParser.ShouldAutoExecuteAfterAutocompleteCommit(line));
    }
}

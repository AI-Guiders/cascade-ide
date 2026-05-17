using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashAutocompleteTests
{
    [Fact]
    public void GetSuggestions_EmptySlash_ReturnsAll()
    {
        var all = ChatSlashCommandCatalog.AllSuggestions();
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/");
        Assert.Equal(all.Count, suggestions.Count);
    }

    [Fact]
    public void GetSuggestions_NotSlash_ReturnsEmpty()
    {
        Assert.Empty(ChatSlashAutocomplete.GetSuggestions("hello"));
    }

    [Fact]
    public void GetSuggestions_PrefixBuild_FiltersNamespaceCommands()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/b");
        Assert.Contains(suggestions, s => s.SlashPath == "/build run");
        Assert.Contains(suggestions, s => s.SlashPath == "/build ui");
        Assert.DoesNotContain(suggestions, s => s.SlashPath == "/overview");
    }

    [Fact]
    public void GetSuggestions_BuildNamespace_ListsActions()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/build ");
        Assert.Equal(2, suggestions.Count);
        Assert.Contains(suggestions, s => s.SlashPath == "/build run");
        Assert.Contains(suggestions, s => s.SlashPath == "/build ui");
    }

    [Fact]
    public void GetSuggestions_BuildActionPrefix_FiltersRun()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/build r");
        Assert.Single(suggestions);
        Assert.Equal("/build run", suggestions[0].SlashPath);
    }
}

#nullable enable
using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashSemanticCatalogIndexTests
{
    [Fact]
    public void GetSuggestions_Root_UsesSemanticDomainsAndElisionStarters()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/");
        var titles = suggestions.Select(s => s.ListTitle).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("intercom", titles);
        Assert.Contains("build", titles);
        Assert.Contains("git", titles);
        Assert.DoesNotContain("run", titles);
    }

    [Fact]
    public void GetSuggestions_BuildSpace_ListsIntentsNotObjects()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/build ");
        Assert.All(suggestions, s => Assert.DoesNotContain("build", s.ListTitle, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(suggestions, s => s.ListTitle == "run");
        Assert.Contains(suggestions, s => s.ListTitle == "ui");
    }

    [Fact]
    public void GetSuggestions_IntercomSpace_ListsObjectsNotIntents()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/intercom ");
        Assert.Contains(suggestions, s => s.ListTitle == "topic");
        Assert.Contains(suggestions, s => s.ListTitle == "server");
        Assert.DoesNotContain(suggestions, s => s.ListTitle is "list" or "rename");
    }
}

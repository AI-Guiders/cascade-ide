using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MessageAnchorSlashCompletionProviderTests
{
    [Fact]
    public void GetMatches_returns_ordinals_for_selected_message_anchors()
    {
        var provider = new MessageAnchorSlashCompletionProvider(() =>
        [
            new AttachmentAnchor { Id = "abcd1234", DisplayLabel = "Foo" },
            new AttachmentAnchor { Id = "ef005678", DisplayLabel = "Bar" },
        ]);

        var matches = provider.GetMatches("", limit: 10);
        Assert.Equal(2, matches.Count);
        Assert.Equal("1", matches[0].InsertArg);
        Assert.Contains("Foo", matches[0].Label, StringComparison.Ordinal);
        Assert.Equal("2", matches[1].InsertArg);
    }

    [Fact]
    public void Autocomplete_anchor_peek_offers_ordinals_after_message_select()
    {
        var provider = new MessageAnchorSlashCompletionProvider(() =>
        [
            new AttachmentAnchor { Id = "abcd1234", DisplayLabel = "Method" },
        ]);

        var suggestions = ChatSlashAutocomplete.GetSuggestions(
            "/anchor peek ",
            messageAnchors: provider);

        Assert.Single(suggestions);
        Assert.Equal("/anchor peek 1", suggestions[0].InsertText);
    }
}

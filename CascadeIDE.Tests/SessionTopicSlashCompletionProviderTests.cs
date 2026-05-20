using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SessionTopicSlashCompletionProviderTests
{
    private static readonly Guid MainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void GetMatches_LabelShowsIdAndTitle_InsertArgIsShortId()
    {
        var provider = new SessionTopicSlashCompletionProvider(SampleSnapshot);
        var matches = provider.GetMatches("", limit: 10);
        var branch = Assert.Single(matches, m => m.InsertArg == "bbbbbbbb");
        Assert.Equal("bbbbbbbb · Ветка", branch.Label);
        Assert.Contains("сообщ.", branch.Help);
    }

    [Fact]
    public void Autocomplete_TopicOpen_UsesLabelAsDisplayLine()
    {
        var provider = new SessionTopicSlashCompletionProvider(SampleSnapshot);
        var suggestions = ChatSlashAutocomplete.GetSuggestions(
            "/intercom topic open ",
            sessionTopics: provider);
        Assert.Contains(suggestions, s =>
            s.InsertText == "/intercom topic open bbbbbbbb"
            && s.SlashPath == "bbbbbbbb · Ветка");
    }

    [Fact]
    public void GetMatches_FiltersByIdPrefix()
    {
        var provider = new SessionTopicSlashCompletionProvider(SampleSnapshot);
        var matches = provider.GetMatches("bbbb", limit: 10);
        Assert.Single(matches);
        Assert.Equal("bbbbbbbb", matches[0].InsertArg);
    }

    private static ChatSurfaceSnapshot SampleSnapshot() =>
        new(
            new ChatSurfaceState(
                [
                    new ChatThreadNode(MainId, "t-main", "Основная тема", true, true, null, null, 0, 0),
                    new ChatThreadNode(BranchId, "t-branch", "Ветка", false, false, MainId, null, 1, 1),
                ],
                [],
                [],
                [],
                MainId,
                "Chat"),
            new ChatSurfaceLayout(
                [
                    new ChatThreadOverviewItem(MainId, "Основная тема", "", true, true, 0, 2),
                    new ChatThreadOverviewItem(BranchId, "Ветка", "", false, false, 1, 1),
                ],
                []),
            ChatProductSpine.Empty);
}

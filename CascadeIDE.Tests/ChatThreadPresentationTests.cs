using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatThreadPresentationTests
{
    private static readonly Guid MainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void BuildPickerRows_List_OrdersByOrder()
    {
        var main = new ChatThreadNode(MainId, "t-main", "Main", true, true, null, null, 0, 0);
        var branch = new ChatThreadNode(BranchId, "t-branch", "Branch", false, false, MainId, null, 1, 1);
        var counts = new Dictionary<Guid, int> { [MainId] = 2, [BranchId] = 1 };

        var rows = ChatThreadPresentation.BuildPickerRows(
            TopicPickerPresentation.List,
            [main, branch],
            counts);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Main", rows[0].Title);
        Assert.Equal("Branch", rows[1].Title);
    }

    [Fact]
    public void RankThreadsForCompletion_PrefixMatchesIdFirst()
    {
        var main = new ChatThreadNode(MainId, "t-main", "Alpha", true, true, null, null, 0, 0);
        var branch = new ChatThreadNode(BranchId, "t-branch", "Beta", false, false, null, null, 1, 1);
        var ranked = ChatThreadPresentation.RankThreadsForCompletion([main, branch], "bbbbbbbb").ToList();
        Assert.Single(ranked);
        Assert.Equal(BranchId, ranked[0].ThreadId);
    }

    [Fact]
    public void MessageCountsByThread_UsesStateMessages()
    {
        var main = new ChatThreadNode(MainId, "t-main", "Main", true, true, null, null, 0, 0);
        var branch = new ChatThreadNode(BranchId, "t-branch", "Branch", false, false, MainId, null, 1, 1);
        var msgMain = new ChatMessageNode(
            Guid.NewGuid(), "m1", MainId, null, 0, "user", "hi", false, false, null, null, null);
        var msgBranch = new ChatMessageNode(
            Guid.NewGuid(), "m2", BranchId, null, 1, "user", "x", false, false, null, null, null);
        var state = new ChatSurfaceState([main, branch], [msgMain, msgBranch], [], [], MainId, "Chat");
        var layout = new ChatSurfaceLayout([], []);
        var snapshot = new ChatSurfaceSnapshot(state, layout, ChatProductSpine.Empty);

        var counts = ChatThreadPresentation.MessageCountsByThread(snapshot);
        Assert.Equal(1, counts[MainId]);
        Assert.Equal(1, counts[BranchId]);
    }
}

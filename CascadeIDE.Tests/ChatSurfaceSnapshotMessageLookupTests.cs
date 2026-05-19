#nullable enable
using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSurfaceSnapshotMessageLookupTests
{
    [Fact]
    public void TryGetMessageBody_finds_entry_in_lane()
    {
        var snapshot = new ChatSurfaceSnapshot(
            ChatSurfaceSnapshot.Empty.State,
            new ChatSurfaceLayout(
                [],
                [
                    new ChatSurfaceLane(
                        new ChatThreadNode(Guid.NewGuid(), "t1", "T", true, true, null, null, 0, 0),
                        [
                            new ChatSurfaceEntry(
                                ChatSurfaceEntryKind.Message,
                                "n1",
                                "Title",
                                "hello body",
                                ChatMessageVisualRole.Assistant,
                                0,
                                MessageIndex: 3)
                        ])
                ]),
            ChatProductSpine.Empty);

        Assert.Equal("hello body", ChatSurfaceSnapshotMessageLookup.TryGetMessageBody(snapshot, 3));
        Assert.Null(ChatSurfaceSnapshotMessageLookup.TryGetMessageBody(snapshot, 99));
    }
}

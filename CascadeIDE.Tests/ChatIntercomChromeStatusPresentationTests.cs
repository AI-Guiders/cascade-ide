using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatIntercomChromeStatusPresentationTests
{
    [Fact]
    public void FormatSubtitle_DetailMode_IncludesTopicLineAndCount()
    {
        var threadId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var snapshot = new ChatSurfaceSnapshot(
            new ChatSurfaceState([], [], [], [], threadId, "Chat"),
            new ChatSurfaceLayout(
                [
                    new ChatThreadOverviewItem(threadId, "Основная тема", "summary", true, true, 0, 4),
                ],
                [
                    new ChatSurfaceLane(
                        new ChatThreadNode(threadId, "t1", "Основная тема", true, true, null, null, 0, 0),
                        [
                            new ChatSurfaceEntry(
                                ChatSurfaceEntryKind.Message,
                                "m1",
                                "user",
                                "hi",
                                ChatMessageVisualRole.User,
                                0,
                                0),
                        ]),
                ]),
            new ChatProductSpine("Продуктовая линия", "focus", [], false));

        var line = ChatIntercomChromeStatusPresentation.FormatSubtitle(snapshot, overviewMode: false, threadId);
        Assert.NotNull(line);
        Assert.Contains("тема: Основная тема", line);
        Assert.Contains("линия: Продуктовая линия", line);
        Assert.Contains("сообщений: 1", line);
    }
}

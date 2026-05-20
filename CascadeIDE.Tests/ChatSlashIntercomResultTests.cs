using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashIntercomResultTests
{
    [Fact]
    public async Task TryRun_TopicCreateEmpty_Fails()
    {
        var runner = new ChatSlashCommandRunner(
            (_, _, _) => Task.FromResult(""),
            getChatSurfaceSnapshot: () => ChatSurfaceSnapshot.Empty,
            selectChatThread: _ => { },
            setChatOverviewMode: _ => { },
            createTopicWithTitle: title =>
                string.IsNullOrWhiteSpace(title)
                    ? TopicCreateResult.Fail("Укажи заголовок: /intercom topic create <название>")
                    : TopicCreateResult.Ok("ok"));

        var result = await runner.TryRunAsync("/intercom topic create ");

        Assert.True(result.Handled);
        Assert.False(result.Success);
        Assert.Contains("Укажи заголовок", result.DetailText ?? "");
    }

    [Fact]
    public async Task TryRun_TopicOpenUnknown_Fails()
    {
        var runner = new ChatSlashCommandRunner(
            (_, _, _) => Task.FromResult(""),
            getChatSurfaceSnapshot: () => ChatSurfaceSnapshot.Empty,
            selectChatThread: _ => { },
            setChatOverviewMode: _ => { });

        var result = await runner.TryRunAsync("/intercom topic open missing");

        Assert.True(result.Handled);
        Assert.False(result.Success);
    }
}

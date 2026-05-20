using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSessionStoreTests
{
    [Fact]
    public async Task ResolveOrCreateCurrentSessionId_ReusesPointer()
    {
        var root = CreateTempWorkspaceRoot();
        var store = new ChatSessionStore(root);
        var first = await store.ResolveOrCreateCurrentSessionIdAsync();
        var second = await store.ResolveOrCreateCurrentSessionIdAsync();
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task ResolveOrCreateCurrentSessionId_PicksLatestMetaWhenPointerMissing()
    {
        var root = CreateTempWorkspaceRoot();
        var store = new ChatSessionStore(root);
        var olderId = Guid.Parse("11111111111111111111111111111111");
        var newerId = Guid.Parse("22222222222222222222222222222222");
        var now = DateTimeOffset.UtcNow;
        await store.SaveMetadataAsync(new ChatSessionMetadata(olderId, now.AddHours(-2), now.AddHours(-1)), CancellationToken.None);
        await store.SaveMetadataAsync(new ChatSessionMetadata(newerId, now.AddHours(-1), now), CancellationToken.None);

        var resolved = await store.ResolveOrCreateCurrentSessionIdAsync();
        Assert.Equal(newerId, resolved);
    }

    [Fact]
    public async Task ReadEventsAsync_RoundTripsAppendOnlyLog()
    {
        var root = CreateTempWorkspaceRoot();
        var store = new ChatSessionStore(root);
        var sessionId = await store.ResolveOrCreateCurrentSessionIdAsync();
        var ev = new ChatHistoryEvent(
            Guid.NewGuid(),
            sessionId,
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.MessageAdded,
            ChatHistoryJson.Serialize(new ChatHistoryMessagePayload(
                Guid.NewGuid().ToString("N"),
                "user",
                "hello",
                Guid.NewGuid().ToString("N"))));
        await store.AppendEventAsync(ev, CancellationToken.None);

        var rows = await store.ReadEventsAsync(sessionId, CancellationToken.None);
        Assert.Single(rows);
        Assert.Equal(ev.EventId, rows[0].EventId);
    }

    [Fact]
    public async Task ResolveOrCreateCurrentSessionId_PicksLatestEventsWhenNoMeta()
    {
        var root = CreateTempWorkspaceRoot();
        var store = new ChatSessionStore(root);
        var olderId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var newerId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var sessionsDir = Path.Combine(root, ".cascade-ide", "chat-sessions");
        Directory.CreateDirectory(sessionsDir);
        var olderPath = Path.Combine(sessionsDir, $"session-{olderId:N}.events.ndjson");
        var newerPath = Path.Combine(sessionsDir, $"session-{newerId:N}.events.ndjson");
        await File.WriteAllTextAsync(olderPath, "");
        await Task.Delay(20);
        await File.WriteAllTextAsync(newerPath, "");

        var resolved = await store.ResolveOrCreateCurrentSessionIdAsync();
        Assert.Equal(newerId, resolved);
    }

    [Fact]
    public void SetWorkspaceRoot_SwitchesSessionDirectory()
    {
        var a = CreateTempWorkspaceRoot();
        var b = CreateTempWorkspaceRoot();
        var store = new ChatSessionStore(a);
        Assert.Contains(a, store.RootPath, StringComparison.OrdinalIgnoreCase);
        store.SetWorkspaceRoot(b);
        Assert.Contains(b, store.RootPath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(a, store.RootPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempWorkspaceRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "cascade-chat-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

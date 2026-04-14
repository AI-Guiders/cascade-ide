using System.Text;
using System.Text.Json;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Простое локальное хранилище истории чата.
/// Канон: append-only NDJSON по событиям + отдельный meta.json.
/// </summary>
public sealed class ChatSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string _rootPath;

    public ChatSessionStore(string workspaceRootPath)
    {
        var root = string.IsNullOrWhiteSpace(workspaceRootPath)
            ? Environment.CurrentDirectory
            : workspaceRootPath.Trim();
        _rootPath = Path.Combine(root, ".cascade-ide", "chat-sessions");
    }

    public Guid EnsureSessionId(Guid? preferred = null) => preferred is { } id && id != Guid.Empty ? id : Guid.NewGuid();

    public async Task<ChatSessionMetadata> LoadOrCreateMetadataAsync(Guid sessionId, CancellationToken ct)
    {
        Directory.CreateDirectory(_rootPath);
        var path = MetaPath(sessionId);
        if (File.Exists(path))
        {
            await using var inStream = File.OpenRead(path);
            var existing = await JsonSerializer.DeserializeAsync<ChatSessionMetadata>(inStream, Json, ct).ConfigureAwait(false);
            if (existing is not null)
                return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var meta = new ChatSessionMetadata(sessionId, now, now, Title: "Chat session");
        await SaveMetadataAsync(meta, ct).ConfigureAwait(false);
        return meta;
    }

    public async Task SaveMetadataAsync(ChatSessionMetadata metadata, CancellationToken ct)
    {
        Directory.CreateDirectory(_rootPath);
        await using var outStream = File.Create(MetaPath(metadata.SessionId));
        await JsonSerializer.SerializeAsync(outStream, metadata, Json, ct).ConfigureAwait(false);
    }

    public async Task AppendEventAsync(ChatHistoryEvent ev, CancellationToken ct)
    {
        Directory.CreateDirectory(_rootPath);
        var line = JsonSerializer.Serialize(ev, Json);
        await File.AppendAllTextAsync(EventsPath(ev.SessionId), line + Environment.NewLine, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatHistoryEvent>> ReadEventsAsync(Guid sessionId, CancellationToken ct)
    {
        var path = EventsPath(sessionId);
        if (!File.Exists(path))
            return [];

        var result = new List<ChatHistoryEvent>();
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
                break;
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var ev = JsonSerializer.Deserialize<ChatHistoryEvent>(line, Json);
            if (ev is not null)
                result.Add(ev);
        }

        return result;
    }

    private string EventsPath(Guid sessionId) => Path.Combine(_rootPath, $"session-{sessionId:N}.events.ndjson");

    private string MetaPath(Guid sessionId) => Path.Combine(_rootPath, $"session-{sessionId:N}.meta.json");
}

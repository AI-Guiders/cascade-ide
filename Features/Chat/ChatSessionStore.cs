using System.Text;
using System.Text.Json;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Простое локальное хранилище истории чата.
/// Канон: append-only NDJSON по событиям + отдельный meta.json + current.session.json.
/// </summary>
public sealed class ChatSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private const string CurrentPointerFileName = "current.session.json";

    private string _rootPath = "";

    public ChatSessionStore(string? workspaceRootPath) => SetWorkspaceRoot(workspaceRootPath);

    /// <summary>Каталог <c>{workspace}/.cascade-ide/chat-sessions</c>.</summary>
    public string RootPath => _rootPath;

    public void SetWorkspaceRoot(string? workspaceRootPath)
    {
        var root = string.IsNullOrWhiteSpace(workspaceRootPath)
            ? Environment.CurrentDirectory
            : workspaceRootPath.Trim();
        _rootPath = Path.Combine(root, ".cascade-ide", "chat-sessions");
    }

    /// <summary>
    /// Возвращает стабильный id сессии: указатель → последняя meta по <see cref="ChatSessionMetadata.UpdatedAtUtc"/> → новая сессия.
    /// </summary>
    public async Task<Guid> ResolveOrCreateCurrentSessionIdAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootPath);

        if (TryReadCurrentPointer(out var pointerId) && MetaExists(pointerId))
            return pointerId;

        var latest = FindLatestSessionIdByMetadata();
        if (latest == Guid.Empty)
            latest = FindLatestSessionIdByEventsLog();
        if (latest != Guid.Empty)
        {
            await WriteCurrentPointerAsync(latest, ct).ConfigureAwait(false);
            await EnsureMetadataForSessionAsync(latest, ct).ConfigureAwait(false);
            return latest;
        }

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await WriteCurrentPointerAsync(id, ct).ConfigureAwait(false);
        await SaveMetadataAsync(new ChatSessionMetadata(id, now, now, Title: "Chat session", MainThreadId: Guid.NewGuid()), ct)
            .ConfigureAwait(false);
        return id;
    }

    public async Task BindCurrentSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == Guid.Empty)
            return;
        Directory.CreateDirectory(_rootPath);
        await WriteCurrentPointerAsync(sessionId, ct).ConfigureAwait(false);
    }

    [Obsolete("Use ResolveOrCreateCurrentSessionIdAsync — иначе каждый старт создаёт новую сессию.")]
    public Guid EnsureSessionId(Guid? preferred = null) =>
        preferred is { } id && id != Guid.Empty ? id : Guid.NewGuid();

    public async Task<ChatSessionMetadata> LoadOrCreateMetadataAsync(Guid sessionId, CancellationToken ct = default)
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

    public async Task SaveMetadataAsync(ChatSessionMetadata metadata, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootPath);
        await using var outStream = File.Create(MetaPath(metadata.SessionId));
        await JsonSerializer.SerializeAsync(outStream, metadata, Json, ct).ConfigureAwait(false);
    }

    public async Task AppendEventAsync(ChatHistoryEvent ev, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootPath);
        var line = JsonSerializer.Serialize(ev, Json);
        await File.AppendAllTextAsync(EventsPath(ev.SessionId), line + Environment.NewLine, Encoding.UTF8, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatHistoryEvent>> ReadEventsAsync(Guid sessionId, CancellationToken ct = default)
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
            try
            {
                var ev = JsonSerializer.Deserialize<ChatHistoryEvent>(line, Json);
                if (ev is not null)
                    result.Add(ev);
            }
            catch (JsonException)
            {
                // Пропускаем битые строки NDJSON, остальная история всё равно поднимается.
            }
        }

        return result;
    }

    private bool TryReadCurrentPointer(out Guid sessionId)
    {
        sessionId = Guid.Empty;
        var path = CurrentPointerPath();
        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path);
            var pointer = JsonSerializer.Deserialize<ChatSessionCurrentPointer>(json, Json);
            if (pointer is null || pointer.SessionId == Guid.Empty)
                return false;
            sessionId = pointer.SessionId;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task WriteCurrentPointerAsync(Guid sessionId, CancellationToken ct)
    {
        Directory.CreateDirectory(_rootPath);
        await using var outStream = File.Create(CurrentPointerPath());
        await JsonSerializer.SerializeAsync(outStream, new ChatSessionCurrentPointer(sessionId), Json, ct)
            .ConfigureAwait(false);
    }

    private Guid FindLatestSessionIdByMetadata()
    {
        if (!Directory.Exists(_rootPath))
            return Guid.Empty;

        Guid bestId = Guid.Empty;
        var bestUpdated = DateTimeOffset.MinValue;
        foreach (var path in Directory.EnumerateFiles(_rootPath, "session-*.meta.json"))
        {
            if (!TryParseSessionIdFromMetaFileName(path, out var id))
                continue;
            try
            {
                var json = File.ReadAllText(path);
                var meta = JsonSerializer.Deserialize<ChatSessionMetadata>(json, Json);
                if (meta is null)
                    continue;
                var updated = meta.UpdatedAtUtc;
                if (updated >= bestUpdated)
                {
                    bestUpdated = updated;
                    bestId = meta.SessionId != Guid.Empty ? meta.SessionId : id;
                }
            }
            catch
            {
                // ignore corrupt meta
            }
        }

        return bestId;
    }

    private Guid FindLatestSessionIdByEventsLog()
    {
        if (!Directory.Exists(_rootPath))
            return Guid.Empty;

        Guid bestId = Guid.Empty;
        var bestWrite = DateTime.MinValue;
        foreach (var path in Directory.EnumerateFiles(_rootPath, "session-*.events.ndjson"))
        {
            if (!TryParseSessionIdFromEventsFileName(path, out var id))
                continue;
            var write = File.GetLastWriteTimeUtc(path);
            if (write >= bestWrite)
            {
                bestWrite = write;
                bestId = id;
            }
        }

        return bestId;
    }

    private async Task EnsureMetadataForSessionAsync(Guid sessionId, CancellationToken ct)
    {
        if (MetaExists(sessionId))
            return;
        var events = await ReadEventsAsync(sessionId, ct).ConfigureAwait(false);
        var mainThreadId = ChatHistoryMessageProjector.InferMainThreadId(events);
        var now = DateTimeOffset.UtcNow;
        await SaveMetadataAsync(new ChatSessionMetadata(sessionId, now, now, Title: "Chat session", MainThreadId: mainThreadId), ct)
            .ConfigureAwait(false);
    }

    private static bool TryParseSessionIdFromMetaFileName(string path, out Guid sessionId) =>
        TryParseSessionIdFromSessionFileName(path, ".meta", out sessionId);

    private static bool TryParseSessionIdFromEventsFileName(string path, out Guid sessionId) =>
        TryParseSessionIdFromSessionFileName(path, ".events", out sessionId);

    private static bool TryParseSessionIdFromSessionFileName(string path, string middleSuffix, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        const string prefix = "session-";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        var tail = name[prefix.Length..];
        if (!tail.EndsWith(middleSuffix, StringComparison.OrdinalIgnoreCase))
            return false;
        var hex = tail[..^middleSuffix.Length];
        return Guid.TryParseExact(hex, "N", out sessionId);
    }

    private bool MetaExists(Guid sessionId) =>
        sessionId != Guid.Empty && File.Exists(MetaPath(sessionId));

    private string CurrentPointerPath() => Path.Combine(_rootPath, CurrentPointerFileName);

    private string EventsPath(Guid sessionId) => Path.Combine(_rootPath, $"session-{sessionId:N}.events.ndjson");

    private string MetaPath(Guid sessionId) => Path.Combine(_rootPath, $"session-{sessionId:N}.meta.json");
}

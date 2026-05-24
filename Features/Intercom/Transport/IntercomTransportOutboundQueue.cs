using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Offline outbox: <c>.cascade-ide/intercom-transport-outbox.ndjson</c> (ADR 0144 §9).</summary>
public sealed class IntercomTransportOutboundQueue
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string? _workspaceRoot;

    public void SetWorkspaceRoot(string? workspaceRoot) => _workspaceRoot = workspaceRoot;

    public async Task EnqueueAsync(IntercomOutboundQueueEntry entry, CancellationToken ct = default)
    {
        var path = OutboxPath();
        if (path is null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(entry, Json);
        await File.AppendAllTextAsync(path, line + Environment.NewLine, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IntercomOutboundQueueEntry>> ReadAllAsync(CancellationToken ct = default)
    {
        var path = OutboxPath();
        if (path is null || !File.Exists(path))
            return [];

        var list = new List<IntercomOutboundQueueEntry>();
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
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
                var entry = JsonSerializer.Deserialize<IntercomOutboundQueueEntry>(line, Json);
                if (entry is not null)
                    list.Add(entry);
            }
            catch (JsonException)
            {
                // skip corrupt line
            }
        }

        return list;
    }

    public async Task ReplaceAllAsync(IReadOnlyList<IntercomOutboundQueueEntry> remaining, CancellationToken ct = default)
    {
        var path = OutboxPath();
        if (path is null)
            return;

        if (remaining.Count == 0)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var lines = remaining.Select(e => JsonSerializer.Serialize(e, Json));
        await File.WriteAllTextAsync(path, string.Join(Environment.NewLine, lines) + Environment.NewLine, ct)
            .ConfigureAwait(false);
    }

    private string? OutboxPath()
    {
        if (string.IsNullOrWhiteSpace(_workspaceRoot))
            return null;
        return Path.Combine(_workspaceRoot, ".cascade-ide", "intercom-transport-outbox.ndjson");
    }
}

public sealed record IntercomOutboundQueueEntry(
    [property: JsonPropertyName("topic_id")] string TopicId,
    [property: JsonPropertyName("request")] IntercomAppendEventRequestDto Request,
    [property: JsonPropertyName("attempts")] int Attempts = 0);

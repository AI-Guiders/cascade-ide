using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Состояние transport per workspace: <c>last_seq</c>, mapping thread→topic.</summary>
public sealed class IntercomTransportStateStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private string? _workspaceRoot;
    private long _lastSeq;
    private readonly Dictionary<string, string> _threadTopics = new(StringComparer.OrdinalIgnoreCase);

    public long LastSeq => _lastSeq;

    public void SetWorkspaceRoot(string? workspaceRoot) => _workspaceRoot = workspaceRoot;

    public void UpdateLastSeq(long seq)
    {
        if (seq <= _lastSeq)
            return;
        _lastSeq = seq;
        Persist();
    }

    public bool TryGetTopicForThread(string threadId, out string topicId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
            return _threadTopics.TryGetValue("general", out topicId!);
        return _threadTopics.TryGetValue(threadId, out topicId!);
    }

    public void SetTopicForThread(string threadId, string topicId)
    {
        var key = string.IsNullOrWhiteSpace(threadId) ? "general" : threadId;
        _threadTopics[key] = topicId;
        Persist();
    }

    public void Load()
    {
        _lastSeq = 0;
        _threadTopics.Clear();
        var path = StatePath();
        if (path is null || !File.Exists(path))
            return;

        try
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<StateDto>(json, Json);
            if (state is null)
                return;
            _lastSeq = state.LastSeq;
            if (state.ThreadTopics is not null)
            {
                foreach (var kv in state.ThreadTopics)
                    _threadTopics[kv.Key] = kv.Value;
            }
        }
        catch
        {
            _lastSeq = 0;
            _threadTopics.Clear();
        }
    }

    private void Persist()
    {
        var path = StatePath();
        if (path is null)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var dto = new StateDto(_lastSeq, new Dictionary<string, string>(_threadTopics));
            File.WriteAllText(path, JsonSerializer.Serialize(dto, Json));
        }
        catch
        {
            // best-effort
        }
    }

    private string? StatePath()
    {
        if (string.IsNullOrWhiteSpace(_workspaceRoot))
            return null;
        return Path.Combine(_workspaceRoot, ".cascade-ide", "intercom-transport-state.json");
    }

    private sealed record StateDto(
        [property: JsonPropertyName("last_seq")] long LastSeq,
        [property: JsonPropertyName("thread_topics")] Dictionary<string, string>? ThreadTopics);
}

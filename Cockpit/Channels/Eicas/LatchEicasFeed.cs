#nullable enable
namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>Mutable EICAS feed for dual-HCI latches (agent alert/qrh → glass).</summary>
public sealed class LatchEicasFeed : IEicasFeed
{
    readonly object _gate = new();
    readonly Dictionary<string, IReadOnlyList<EicasMessage>> _sources =
        new(StringComparer.Ordinal);
    IReadOnlyList<EicasMessage> _messages = Array.Empty<EicasMessage>();

    public event EventHandler? MessagesChanged;

    public IReadOnlyList<EicasMessage> GetMessages()
    {
        lock (_gate)
            return _messages;
    }

    /// <summary>Replace the sole source (compat — prefer <see cref="ReplaceSource"/>).</summary>
    public void Replace(IReadOnlyList<EicasMessage> messages) =>
        ReplaceSource("default", messages);

    /// <summary>Replace one named source and recompose (alert, qrh, …).</summary>
    public void ReplaceSource(string source, IReadOnlyList<EicasMessage> messages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        lock (_gate)
        {
            _sources[source] = messages ?? Array.Empty<EicasMessage>();
            _messages = Compose(_sources);
        }

        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }

    static IReadOnlyList<EicasMessage> Compose(
        Dictionary<string, IReadOnlyList<EicasMessage>> sources)
    {
        // Stable order: alert (SA) before qrh (handbook), then others.
        string[] order = ["alert", "qrh", "default"];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<EicasMessage>();
        foreach (var key in order)
        {
            if (!sources.TryGetValue(key, out var msgs) || msgs.Count == 0)
                continue;
            list.AddRange(msgs);
            seen.Add(key);
        }

        foreach (var (key, msgs) in sources)
        {
            if (seen.Contains(key) || msgs.Count == 0)
                continue;
            list.AddRange(msgs);
        }

        return list.Count == 0 ? Array.Empty<EicasMessage>() : list;
    }
}

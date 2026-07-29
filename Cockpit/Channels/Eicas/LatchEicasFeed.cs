#nullable enable
namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>Mutable EICAS feed for dual-HCI latches (agent alert → glass).</summary>
public sealed class LatchEicasFeed : IEicasFeed
{
    readonly object _gate = new();
    IReadOnlyList<EicasMessage> _messages = Array.Empty<EicasMessage>();

    public event EventHandler? MessagesChanged;

    public IReadOnlyList<EicasMessage> GetMessages()
    {
        lock (_gate)
            return _messages;
    }

    public void Replace(IReadOnlyList<EicasMessage> messages)
    {
        lock (_gate)
            _messages = messages ?? Array.Empty<EicasMessage>();
        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }
}

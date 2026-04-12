namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>Заглушка: нет активных оповещений (Dark Cockpit по умолчанию).</summary>
public sealed class EmptyEicasFeed : IEicasFeed
{
    public event EventHandler? MessagesChanged;

    public IReadOnlyList<EicasMessage> GetMessages() => Array.Empty<EicasMessage>();

    /// <summary>Для тестов и будущих реализаций с подпиской.</summary>
    public void NotifyMessagesChanged() => MessagesChanged?.Invoke(this, EventArgs.Empty);
}

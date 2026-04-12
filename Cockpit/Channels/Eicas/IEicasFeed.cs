namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>Источник списка оповещений EICAS. Реализации поднимают <see cref="MessagesChanged"/> при смене набора.</summary>
public interface IEicasFeed
{
    event EventHandler? MessagesChanged;

    /// <summary>Снимок сообщений; упорядочивание для UI — композитор <c>EicasMessageSorter</c> (ADR 0036 п.3).</summary>
    IReadOnlyList<EicasMessage> GetMessages();
}

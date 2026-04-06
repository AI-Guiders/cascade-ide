namespace CascadeIDE.Features.UiChrome;

/// <summary>Источник списка оповещений EICAS. Реализации поднимают <see cref="MessagesChanged"/> при смене набора.</summary>
public interface IEicasFeed
{
    event EventHandler? MessagesChanged;

    /// <summary>Снимок сообщений; сортировка для UI — в <see cref="EicasCompositor"/>.</summary>
    IReadOnlyList<EicasMessage> GetMessages();
}

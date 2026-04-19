namespace CascadeIDE.Services;

/// <summary>Провайдер чата с ИИ: стриминг ответа по списку сообщений.</summary>
public interface IAiChatProvider
{
    IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}

namespace CascadeIDE.Services;

public interface IOllamaService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetModelNamesAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}

public sealed record ChatMessage(string Role, string Content);

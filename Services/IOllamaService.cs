namespace CascadeIDE.Services;

public interface IOllamaService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetModelNamesAsync(CancellationToken cancellationToken = default);
    /// <summary>Детали модели из API: возможности, размер параметров, контекст, лицензия.</summary>
    Task<Models.OllamaModelDetails?> GetModelDetailsAsync(string modelName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
    /// <summary>Скачивает модель через Ollama API (POST /api/pull), стримит статусы.</summary>
    IAsyncEnumerable<string> PullModelAsync(string modelName, CancellationToken cancellationToken = default);
}

public sealed record ChatMessage(string Role, string Content);

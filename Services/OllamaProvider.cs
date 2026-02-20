namespace CascadeIDE.Services;

/// <summary>Провайдер чата через Ollama (обёртка над IOllamaService).</summary>
public sealed class OllamaProvider(IOllamaService ollama) : IAiChatProvider
{
    public IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default)
        => ollama.StreamChatAsync(model, messages, cancellationToken);
}

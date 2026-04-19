using System.Runtime.CompilerServices;

namespace CascadeIDE.Services;

/// <summary>Возвращает провайдер и модель для данного ключа (Ollama, Anthropic, OpenAI, DeepSeek).</summary>
public delegate (IAiChatProvider? Provider, string Model) AiProviderResolver(string providerKey);

/// <summary>Единая точка входа для чата: минимизация контекста (диагностики + сигнатуры) и вызов выбранного провайдера.</summary>
public sealed class AiProviderManager
{
    private readonly ContextMinimizer _minimizer;
    private readonly AiProviderResolver _resolveProvider;

    public AiProviderManager(ContextMinimizer minimizer, AiProviderResolver resolveProvider)
    {
        _minimizer = minimizer;
        _resolveProvider = resolveProvider;
    }

    /// <summary>Стримит ответ от выбранного провайдера. При useMinimizedContext для .cs файла подмешивает блок диагностик и сигнатур.</summary>
    public async IAsyncEnumerable<string> StreamChatAsync(
        string providerKey,
        IReadOnlyList<ChatMessage> messages,
        string? currentFilePath,
        string? currentSourceText,
        bool useMinimizedContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (provider, model) = _resolveProvider(providerKey ?? "Ollama");
        if (provider is null)
        {
            yield return "[Error: provider not configured or API key missing.]";
            yield break;
        }

        var list = messages.AsEnumerable().ToList();
        if (useMinimizedContext && !string.IsNullOrEmpty(currentFilePath) && !string.IsNullOrEmpty(currentSourceText))
        {
            var minimized = _minimizer.Minimize(currentFilePath, currentSourceText, cancellationToken);
            if (!string.IsNullOrEmpty(minimized))
            {
                var contextMessage = "Контекст текущего файла (только диагностики и сигнатуры):\n\n" + minimized;
                list = [new ChatMessage("user", contextMessage), .. list];
            }
        }

        await foreach (var token in provider.StreamChatAsync(model, list, cancellationToken))
            yield return token;
    }
}

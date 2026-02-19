using OllamaSharp;

namespace CascadeIDE.Services;

public sealed class OllamaService : IOllamaService
{
    private readonly Uri _baseUrl;
    private readonly HttpClient _httpClient;

    public OllamaService(Uri? baseUrl = null)
    {
        _baseUrl = baseUrl ?? new Uri("http://localhost:11434");
        _httpClient = new HttpClient { BaseAddress = _baseUrl };
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetModelNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ollama = new OllamaApiClient(_httpClient);
            var models = await ollama.ListLocalModelsAsync(cancellationToken);
            return models.Select(m => m.Name).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ollama = new OllamaApiClient(_httpClient) { SelectedModel = model };
        var prompt = messages.Count > 0 ? messages[^1].Content : "";

        await foreach (var chunk in ollama.GenerateAsync(prompt, cancellationToken: cancellationToken))
        {
            if (chunk?.Response is { } text)
                yield return text;
        }
    }
}

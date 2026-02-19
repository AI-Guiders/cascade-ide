using OllamaSharp;

namespace AgentIde.Services;

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
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace CascadeIDE.Services;

/// <summary>Провайдер чата через OpenAI-совместимый API (OpenAI, DeepSeek и др.).</summary>
public sealed class OpenAiCompatibleProvider : IAiChatProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public OpenAiCompatibleProvider(string baseUrl, string apiKey, string modelId)
    {
        var baseUri = new Uri(baseUrl?.TrimEnd('/') ?? "https://api.openai.com");
        _httpClient = new HttpClient { BaseAddress = baseUri };
        _apiKey = apiKey ?? "";
        _modelId = modelId ?? "gpt-4o";
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
    }

    public async IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrEmpty(model) ? _modelId : model;
        if (string.IsNullOrEmpty(_apiKey))
        {
            yield return "[Error: API key not set.]";
            yield break;
        }

        var requestBody = new
        {
            model = modelId,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return $"[API error {(int)response.StatusCode}: {err}]";
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var json = line.Length > 5 ? line[5..].Trim() : "";
            if (json == "[DONE]" || string.IsNullOrEmpty(json)) continue;
            string? text = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("delta", out var delta)
                    && delta.TryGetProperty("content", out var contentEl))
                    text = contentEl.GetString();
            }
            catch
            {
                // skip invalid line
            }
            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }
}

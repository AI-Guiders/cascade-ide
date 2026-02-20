using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace CascadeIDE.Services;

/// <summary>Провайдер чата через Anthropic API (Claude).</summary>
public sealed class AnthropicProvider : IAiChatProvider
{
    private const string DefaultBaseUrl = "https://api.anthropic.com";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public AnthropicProvider(string apiKey, string modelId, string? baseUrl = null)
    {
        _apiKey = apiKey ?? "";
        _modelId = modelId ?? "claude-sonnet-4-20250514";
        var baseUri = new Uri(baseUrl ?? DefaultBaseUrl);
        _httpClient = new HttpClient { BaseAddress = baseUri };
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrEmpty(model) ? _modelId : model;
        if (string.IsNullOrEmpty(_apiKey))
        {
            yield return "[Error: Anthropic API key not set.]";
            yield break;
        }

        var requestBody = new
        {
            model = modelId,
            max_tokens = 8192,
            stream = true,
            messages = messages.Select(m => new { role = m.Role == "assistant" ? "assistant" : "user", content = m.Content }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return $"[Anthropic error {(int)response.StatusCode}: {err}]";
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
                if (root.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "content_block_delta"
                    && root.TryGetProperty("delta", out var deltaEl)
                    && deltaEl.TryGetProperty("type", out var deltaType) && deltaType.GetString() == "text_delta"
                    && deltaEl.TryGetProperty("text", out var textEl))
                    text = textEl.GetString();
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

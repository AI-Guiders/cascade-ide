using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CascadeIDE.Services.Fm;

namespace CascadeIDE.Services;

/// <summary>Провайдер чата через OpenAI-совместимый API (OpenAI, DeepSeek, Cloud.ru FM и др.).</summary>
public sealed class OpenAiCompatibleProvider : IAiChatProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public OpenAiCompatibleProvider(string baseUrl, string apiKey, string modelId)
    {
        var baseUri = new Uri(NormalizeBaseUrl(baseUrl));
        _httpClient = new HttpClient { BaseAddress = baseUri };
        _apiKey = apiKey ?? "";
        _modelId = modelId ?? "gpt-4o";
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        ChatTurnUsageCollector? usageCollector = null)
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
            stream_options = new { include_usage = true },
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
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

        FmTurnUsage? usage = null;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var json = line.Length > 5 ? line[5..].Trim() : "";
            if (json == "[DONE]" || string.IsNullOrEmpty(json)) continue;

            var parsedUsage = FmOpenAiUsageParser.TryParseFromCompletionChunk(json);
            if (parsedUsage is not null)
                usage = parsedUsage;

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

        if (usage is not null)
            usageCollector?.Report(usage);
    }

    private static string NormalizeBaseUrl(string? baseUrl)
    {
        var t = (baseUrl ?? "").Trim().TrimEnd('/');
        if (t.Length == 0)
            return "https://api.openai.com/v1/";
        if (!t.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            t += "/v1";
        return t + "/";
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using OllamaSharp;

namespace CascadeIDE.Services;

public sealed class OllamaService : IOllamaService
{
    /// <summary>Стандартный HTTP-базис нативного API Ollama (как в документации); совпадает с MAF/OllamaChatClient без кастомного хоста в настройках.</summary>
    public const string DefaultBaseUriString = "http://localhost:11434";

    private readonly Uri _baseUrl;
    private readonly HttpClient _httpClient;

    public OllamaService(Uri? baseUrl = null)
    {
        _baseUrl = baseUrl ?? new Uri(DefaultBaseUriString);
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

    public async Task<Models.OllamaModelDetails?> GetModelDetailsAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/show");
            request.Content = JsonContent.Create(new { model = modelName.Trim() });
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            root.TryGetProperty("details", out var detailsEl);
            root.TryGetProperty("model_info", out var modelInfoEl);

            string? paramSize = null, quant = null, family = null, format = null;
            if (detailsEl.ValueKind == JsonValueKind.Object)
            {
                detailsEl.TryGetProperty("parameter_size", out var ps);
                paramSize = ps.GetString();
                detailsEl.TryGetProperty("quantization_level", out var q);
                quant = q.GetString();
                detailsEl.TryGetProperty("family", out var f);
                family = f.GetString();
                detailsEl.TryGetProperty("format", out var fmt);
                format = fmt.GetString();
            }

            long? contextLength = null;
            if (modelInfoEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in modelInfoEl.EnumerateObject())
                {
                    if (prop.Name.EndsWith("context_length", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt64(out var ctx))
                    {
                        contextLength = ctx;
                        break;
                    }
                }
            }

            var caps = new List<string>();
            if (root.TryGetProperty("capabilities", out var capsEl) && capsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in capsEl.EnumerateArray())
                    if (c.GetString() is { } s)
                        caps.Add(s);
            }

            var license = root.TryGetProperty("license", out var licEl) ? licEl.GetString() : null;
            var modifiedAt = root.TryGetProperty("modified_at", out var modEl) ? modEl.GetString() : null;

            return new Models.OllamaModelDetails
            {
                ParameterSize = paramSize,
                QuantizationLevel = quant,
                Family = family,
                Format = format,
                Capabilities = caps,
                ContextLength = contextLength,
                License = license,
                ModifiedAt = modifiedAt
            };
        }
        catch
        {
            return null;
        }
    }

    public async IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            yield break;
        var requestBody = new
        {
            model,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
        request.Content = JsonContent.Create(requestBody);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string? text = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                    text = content.GetString();
            }
            catch
            {
                // skip
            }
            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }

    public async IAsyncEnumerable<string> PullModelAsync(string modelName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            yield break;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/pull");
        request.Content = JsonContent.Create(new { model = modelName.Trim(), stream = true });

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var status = ParsePullStatusLine(line);
            if (!string.IsNullOrEmpty(status))
                yield return status;
        }
    }

    private static string? ParsePullStatusLine(string jsonLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var statusEl))
            {
                var status = statusEl.GetString() ?? "";
                if (root.TryGetProperty("completed", out var completed) && root.TryGetProperty("total", out var total))
                {
                    var c = completed.TryGetInt64(out var cv) ? cv : 0L;
                    var t = total.TryGetInt64(out var tv) ? tv : 0L;
                    if (t > 0)
                        return $"{status} {(int)(c * 100 / t)}%";
                }
                return status;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }
}

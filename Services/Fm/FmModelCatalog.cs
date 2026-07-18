using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CascadeIDE.Services.Fm;

/// <summary>Кэш <c>GET /v1/models</c> для OpenAI-compatible FM (Cloud.ru и др.).</summary>
public sealed class FmModelCatalog
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<FmModelInfo?> TryResolveModelAsync(
        string baseUrl,
        string apiKey,
        string modelId,
        CancellationToken cancellationToken = default)
    {
        var model = (modelId ?? "").Trim();
        if (model.Length == 0 || string.IsNullOrWhiteSpace(apiKey))
            return null;

        var catalog = await TryLoadCatalogAsync(baseUrl, apiKey, cancellationToken).ConfigureAwait(false);
        return catalog?.FirstOrDefault(m => string.Equals(m.ModelId, model, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<FmModelInfo>?> TryLoadCatalogAsync(
        string baseUrl,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var cacheKey = BuildCacheKey(baseUrl, apiKey);
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresUtc > DateTimeOffset.UtcNow)
            return cached.Models;

        try
        {
            var endpoint = NormalizeBaseUrl(baseUrl);
            using var client = new HttpClient { BaseAddress = new Uri(endpoint) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await client.GetAsync("v1/models", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return cached?.Models;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var models = ParseModelList(doc.RootElement);
            Cache[cacheKey] = new CacheEntry(models, DateTimeOffset.UtcNow.Add(DefaultTtl));
            return models;
        }
        catch
        {
            return cached?.Models;
        }
    }

    internal static IReadOnlyList<FmModelInfo> ParseModelList(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<FmModelInfo>();
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                continue;

            var id = idEl.GetString()?.Trim();
            if (string.IsNullOrEmpty(id))
                continue;

            int? maxLen = null;
            if (item.TryGetProperty("max_model_len", out var maxEl) && maxEl.TryGetInt32(out var max))
                maxLen = max;

            double? promptCost = null;
            double? genCost = null;
            if (item.TryGetProperty("metadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
            {
                if (meta.TryGetProperty("prompt_tokens_cost", out var pc) && pc.TryGetDouble(out var pcd))
                    promptCost = pcd;
                if (meta.TryGetProperty("generated_tokens_cost", out var gc) && gc.TryGetDouble(out var gcd))
                    genCost = gcd;
            }

            list.Add(new FmModelInfo(id, maxLen, promptCost, genCost));
        }

        return list;
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var t = (baseUrl ?? "").Trim().TrimEnd('/');
        if (t.Length == 0)
            t = "https://api.openai.com";
        if (!t.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            t += "/v1";
        return t + "/";
    }

    private static string BuildCacheKey(string baseUrl, string apiKey)
    {
        var url = NormalizeBaseUrl(baseUrl);
        var keyTail = apiKey.Trim();
        keyTail = keyTail.Length <= 8 ? keyTail : keyTail[^8..];
        return url + "|" + keyTail;
    }

    private sealed record CacheEntry(IReadOnlyList<FmModelInfo> Models, DateTimeOffset ExpiresUtc);
}

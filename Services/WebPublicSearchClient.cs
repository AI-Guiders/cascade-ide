#nullable enable

using System.Net;
using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Одноразовый запрос «мгновенного ответа» DuckDuckGo (HTTPS, без API-ключа).
/// Не предназначено для высокочастотного скрейпинга; для редких справок агента.
/// </summary>
internal static class WebPublicSearchClient
{
    private const string DdgJsonUriPrefix = "https://api.duckduckgo.com/?q=";
    private const string DdgJsonUriSuffix = "&format=json&no_html=1&skip_disambig=1";

    private static readonly HttpClient Http = CreateHttpClient();

    private static JsonSerializerOptions ResponseJsonOpts { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(22) };
        http.DefaultRequestVersion = HttpVersion.Version20;
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CascadeIDE/1.0 (web-public-query)");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        return http;
    }

    /// <returns>Компактный JSON с полями в camelCase или объект с ошибкой (<c>offline_or_error</c>).</returns>
    internal static async Task<string> FetchDdgInstantAnswerJsonAsync(string query, CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? "";
        if (trimmed.Length == 0)
            return JsonSerializer.Serialize(new { query = trimmed, offline_or_error = "empty query" }, ResponseJsonOpts);

        var url = DdgJsonUriPrefix + Uri.EscapeDataString(trimmed) + DdgJsonUriSuffix;

        try
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return JsonSerializer.Serialize(new { query = trimmed, offline_or_error = $"HTTP {(int)response.StatusCode}", body_preview = TrimPreview(body), backend = "duckduckgo_instant_answer" }, ResponseJsonOpts);

            using JsonDocument doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string heading = JsonPropString(root, "Heading");
            string abstractText = JsonPropString(root, "AbstractText");
            string abstractUrl = JsonPropString(root, "AbstractURL");
            string? answer = null;
            if (root.TryGetProperty("Answer", out var ans) && ans.ValueKind == JsonValueKind.String)
            {
                var s = ans.GetString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                    answer = s;
            }

            var related = new List<Dictionary<string, string>>(12);
            var remain = 40;
            if (root.TryGetProperty("RelatedTopics", out var rtRoot) && rtRoot.ValueKind == JsonValueKind.Array)
                AppendRelatedLeaves(rtRoot, related, ref remain);

            var payload = new
            {
                query = trimmed,
                heading,
                abstract_text = abstractText,
                abstract_url = abstractUrl,
                answer,
                related_topics = related,
                backend = "duckduckgo_instant_answer",
                note = related.Count == 0 && abstractText.Length == 0 && string.IsNullOrEmpty(answer)
                    ? "Ответ может быть пустым: переформулируй или сузь запрос."
                    : (string?)null,
            };

            return JsonSerializer.Serialize(payload, ResponseJsonOpts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { query = trimmed, offline_or_error = ex.Message, backend = "duckduckgo_instant_answer" }, ResponseJsonOpts);
        }
    }

    private static string JsonPropString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String)
            return "";
        return p.GetString()?.Trim() ?? "";
    }

    /// <summary>DDG кладёт в <c>RelatedTopics</c> смесь листьев (<c>Text</c>/<c>FirstURL</c>) и групп с <c>Topics</c>: [...].</summary>
    private static void AppendRelatedLeaves(JsonElement array, List<Dictionary<string, string>> sink, ref int remaining)
    {
        foreach (var el in array.EnumerateArray())
        {
            if (remaining <= 0)
                break;
            if (el.ValueKind != JsonValueKind.Object)
                continue;

            if (el.TryGetProperty("Topics", out var nested) && nested.ValueKind == JsonValueKind.Array)
            {
                AppendRelatedLeaves(nested, sink, ref remaining);
                continue;
            }

            string text = JsonPropString(el, "Text");
            string url = JsonPropString(el, "FirstURL");
            if (text.Length == 0 && url.Length == 0)
                continue;
            sink.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["text"] = text.Length > 500 ? text[..500] + "…" : text,
                ["url"] = url,
            });
            remaining--;
        }
    }

    private static string TrimPreview(string s, int max = 280) =>
        s.Length <= max ? s : s[..max] + "…";
}

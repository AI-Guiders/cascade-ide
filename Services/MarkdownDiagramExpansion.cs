using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Заменяет в Markdown fenced-блоки <c>```mermaid</c> / <c>```plantuml</c> на встроенные SVG через <a href="https://kroki.io">Kroki</a>.
/// </summary>
public static class MarkdownDiagramExpansion
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    private static readonly SemaphoreSlim KrokiParallel = new(4, 4);

    private static readonly ConcurrentDictionary<string, byte[]> SvgCache = new(StringComparer.Ordinal);

    private static readonly System.Text.RegularExpressions.Regex DiagramFenceRegex = new(
        @"```\s*(mermaid|plantuml|puml)\s*\r?\n([\s\S]*?)```",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    /// <summary>Расширить markdown: блоки mermaid/plantuml → <c>![…](data:image/svg+xml;base64,…)</c>.</summary>
    public static async Task<string> ExpandAsync(string markdown, CascadeIdeSettings settings, CancellationToken cancellationToken = default)
    {
        if (!settings.MarkdownKrokiEnabled || string.IsNullOrEmpty(markdown))
            return markdown;

        var baseUrl = string.IsNullOrWhiteSpace(settings.MarkdownKrokiBaseUrl)
            ? "https://kroki.io"
            : settings.MarkdownKrokiBaseUrl.Trim();

        var matches = DiagramFenceRegex.Matches(markdown);
        if (matches.Count == 0)
            return markdown;

        var tasks = new List<Task<BlockReplace>>(matches.Count);
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            if (!m.Success)
                continue;
            var lang = m.Groups[1].Value.ToLowerInvariant();
            var body = m.Groups[2].Value;
            var diagramType = lang == "puml" ? "plantuml" : lang;
            tasks.Add(ReplaceOneAsync(baseUrl, diagramType, body, m.Index, m.Length, m.Value, cancellationToken));
        }

        if (tasks.Count == 0)
            return markdown;

        var blocks = await Task.WhenAll(tasks).ConfigureAwait(false);
        Array.Sort(blocks, (a, b) => a.Index.CompareTo(b.Index));

        var sb = new StringBuilder(markdown.Length + blocks.Length * 200);
        var copyFrom = 0;
        foreach (var b in blocks)
        {
            sb.Append(markdown, copyFrom, b.Index - copyFrom);
            sb.Append(b.Replacement);
            copyFrom = b.Index + b.Length;
        }

        sb.Append(markdown, copyFrom, markdown.Length - copyFrom);
        return sb.ToString();
    }

    private sealed record BlockReplace(int Index, int Length, string Replacement);

    private static async Task<BlockReplace> ReplaceOneAsync(
        string krokiBaseUrl,
        string diagramType,
        string body,
        int index,
        int length,
        string originalFence,
        CancellationToken cancellationToken)
    {
        var trimmed = body.TrimEnd();
        if (trimmed.Length == 0)
            return new BlockReplace(index, length, originalFence);

        try
        {
            await KrokiParallel.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var cacheKey = CacheKey(krokiBaseUrl, diagramType, trimmed);
                if (!SvgCache.TryGetValue(cacheKey, out var svgBytes))
                {
                    svgBytes = await FetchSvgFromKrokiAsync(krokiBaseUrl, diagramType, trimmed, cancellationToken).ConfigureAwait(false);
                    SvgCache[cacheKey] = svgBytes;
                }

                var b64 = Convert.ToBase64String(svgBytes);
                var label = diagramType == "mermaid" ? "Mermaid" : "PlantUML";
                var md = $"\n\n![{label}](data:image/svg+xml;base64,{b64})\n\n";
                return new BlockReplace(index, length, md);
            }
            finally
            {
                KrokiParallel.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new BlockReplace(
                index,
                length,
                "\n\n> **Диаграмма:** не удалось отрисовать через Kroki. Показан исходник.\n\n" + originalFence + "\n\n");
        }
    }

    private static string CacheKey(string baseUrl, string diagramType, string body)
    {
        var payload = $"{baseUrl}\n{diagramType}\n{body}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static async Task<byte[]> FetchSvgFromKrokiAsync(string krokiBaseUrl, string diagramType, string source, CancellationToken cancellationToken)
    {
        var url = krokiBaseUrl.TrimEnd('/') + "/";
        var payload = new KrokiJsonBody
        {
            DiagramSource = source,
            DiagramType = diagramType,
            OutputFormat = "svg"
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = JsonContent.Create(payload);
        req.Headers.Accept.ParseAdd("image/svg+xml");

        var resp = await Http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class KrokiJsonBody
    {
        [JsonPropertyName("diagram_source")]
        public string DiagramSource { get; set; } = "";

        [JsonPropertyName("diagram_type")]
        public string DiagramType { get; set; } = "";

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; } = "svg";
    }
}

#nullable enable

using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CascadeIDE.Services;

/// <summary>
/// Одиночный HTTP GET по HTTPS с лимитами размера; грубое извлечение текста из HTML (аналог «Fetch» для агента).
/// </summary>
internal static partial class WebPublicDocumentFetchClient
{
    internal const int DefaultMaxChars = 200_000;
    private const int DefaultMaxDownloadBytes = 2 * 1024 * 1024;

    private static readonly HttpClient Http = CreateHttpClient();

    private static readonly JsonSerializerOptions ResponseJsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        http.DefaultRequestVersion = HttpVersion.Version20;
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CascadeIDE/1.0 (fetch-web-public-url)");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,text/plain,application/json,*/*;q=0.8");
        return http;
    }

    /// <returns>JSON: url, resolved_url, status_code, content_type, text, truncated_download, truncated_text, extraction, error?, note.</returns>
    internal static async Task<string> FetchAsync(string urlRaw, int? maxChars, CancellationToken cancellationToken)
    {
        var maxOut = maxChars is > 0 and <= 1_000_000 ? maxChars.Value : DefaultMaxChars;

        var trimmed = urlRaw?.Trim() ?? "";
        if (trimmed.Length == 0)
            return ErrorJson(trimmed, null, "empty url", "Передай непустой absolute https URL.");

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return ErrorJson(trimmed, null, "invalid url", "Нужен абсолютный URL.");

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return ErrorJson(trimmed, uri.ToString(), "https only", "Разрешена только схема https.");

        var host = uri.IdnHost;
        if (IsBlockedEndpointHost(host))
            return ErrorJson(trimmed, uri.ToString(), "host not allowed",
                "Локальные и частные узлы недоступны (базовая защита от SSRF).");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            var resolved = response.RequestMessage?.RequestUri?.ToString() ?? uri.ToString();
            var status = (int)response.StatusCode;

            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var (body, truncatedDl) = await ReadCappedAsync(networkStream, DefaultMaxDownloadBytes, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var preview = TrimForPreview(body.Length == 0 ? "" : ProbeDecode(body));
                return JsonSerializer.Serialize(new
                {
                    url = trimmed,
                    resolved_url = resolved,
                    status_code = status,
                    content_type = ContentTypeBrief(response.Content.Headers.ContentType),
                    text = "",
                    truncated_download = truncatedDl,
                    truncated_text = false,
                    extraction = "none",
                    error = $"HTTP {status}",
                    body_preview = preview,
                    note = "Для ошибок протокола смотри status_code и body_preview.",
                }, ResponseJsonOpts);
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
            var charset = response.Content.Headers.ContentType?.CharSet;
            var decoded = DecodeBody(body, charset);
            string text;
            var extraction = "plain";

            if (LooksLikeHtml(decoded, mediaType))
            {
                text = HtmlToReadableText(decoded);
                extraction = "html_to_text";
            }
            else
            {
                text = decoded;
                if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
                    extraction = "json_as_text";
            }

            NormalizeWhitespace(ref text);

            var truncatedText = text.Length > maxOut;
            if (truncatedText)
                text = text[..maxOut] + "…";

            return JsonSerializer.Serialize(new
            {
                url = trimmed,
                resolved_url = resolved,
                status_code = status,
                content_type = ContentTypeBrief(response.Content.Headers.ContentType),
                text,
                truncated_download = truncatedDl,
                truncated_text = truncatedText,
                extraction,
                note = "Упрощённое извлечение из HTML; сложная вёрстка и скрипты могут дать шум. Редиректы следуются — resolved_url может отличаться.",
            }, ResponseJsonOpts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorJson(trimmed, trimmed, ex.Message, null);
        }
    }

    private static string ErrorJson(string url, string? resolvedUrl, string error, string? note) =>
        JsonSerializer.Serialize(new
        {
            url,
            resolved_url = resolvedUrl,
            error,
            text = "",
            truncated_download = false,
            truncated_text = false,
            extraction = "none",
            note,
        }, ResponseJsonOpts);

    private static bool IsBlockedEndpointHost(string host)
    {
        if (host.Length == 0)
            return true;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (IPAddress.TryParse(host, out var ip))
            return IsPrivateOrLocalIp(ip);

        return false;
    }

    private static bool IsPrivateOrLocalIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 10)
                return true;
            if (b[0] == 127)
                return true;
            if (b[0] == 0)
                return true;
            if (b[0] == 169 && b[1] == 254)
                return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                return true;
            if (b[0] == 192 && b[1] == 168)
                return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal)
                return true;
            // Unique local (fc00::/7)
            var s = ip.ToString();
            if (s.StartsWith("fc", StringComparison.OrdinalIgnoreCase) || s.StartsWith("fd", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task<(byte[] Body, bool Truncated)> ReadCappedAsync(Stream stream, int maxBytes, CancellationToken ct)
    {
        var rent = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            await using var ms = new MemoryStream(capacity: Math.Min(maxBytes, 65536));
            var total = 0;
            while (true)
            {
                var read = await stream.ReadAsync(rent.AsMemory(0, rent.Length), ct).ConfigureAwait(false);
                if (read == 0)
                    break;
                var next = total + read;
                if (next > maxBytes)
                {
                    var allow = maxBytes - total;
                    if (allow > 0)
                        ms.Write(rent, 0, allow);
                    return (ms.ToArray(), true);
                }

                ms.Write(rent, 0, read);
                total = next;
            }

            return (ms.ToArray(), false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    private static string ProbeDecode(byte[] body) => DecodeBody(body, null);

    private static string DecodeBody(byte[] body, string? headerCharset)
    {
        if (body.Length == 0)
            return "";

        Encoding? enc = null;
        if (!string.IsNullOrWhiteSpace(headerCharset))
        {
            try { enc = Encoding.GetEncoding(headerCharset); }
            catch (ArgumentException) { /* use UTF-8 */ }
        }

        if (enc is null)
        {
            if (body.Length >= 3 && body[0] == 0xEF && body[1] == 0xBB && body[2] == 0xBF)
                return Encoding.UTF8.GetString(body.AsSpan(3));
            if (body.Length >= 2 && body[0] == 0xFF && body[1] == 0xFE)
                return Encoding.Unicode.GetString(body.AsSpan(2));
            return Encoding.UTF8.GetString(body);
        }

        return enc.GetString(body);
    }

    private static string ContentTypeBrief(MediaTypeHeaderValue? mt)
    {
        if (mt is null)
            return "";
        var s = mt.MediaType ?? "";
        if (!string.IsNullOrEmpty(mt.CharSet))
            s += "; charset=" + mt.CharSet;
        return s;
    }

    private static bool LooksLikeHtml(string s, string mediaType)
    {
        if (mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            return true;
        var t = s.AsSpan().TrimStart();
        return t.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
               || t.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
               || t.StartsWith("<HTML", StringComparison.Ordinal);
    }

    private static string HtmlToReadableText(string html)
    {
        var s = html;

        s = ScriptStyleRx().Replace(s, " ");
        s = BlockCloseRx().Replace(s, "\n");
        s = TagRx().Replace(s, " ");
        s = WebUtility.HtmlDecode(s);
        return s;
    }

    private static void NormalizeWhitespace(ref string s)
    {
        s = LineBreakRx().Replace(s, "\n");
        s = MultiSpaceRx().Replace(s, " ");
        s = MultiNlRx().Replace(s, "\n\n");
        s = s.Trim();
    }

    private static string TrimForPreview(string s, int max = 400) =>
        s.Length <= max ? s : s[..max] + "…";

    [GeneratedRegex(@"<script\b[^>]*>.*?</script>|<style\b[^>]*>.*?</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRx();

    [GeneratedRegex(@"</(p|div|br|tr|h[1-6]|li|section|article|header|footer|main)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockCloseRx();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex TagRx();

    [GeneratedRegex(@"\r\n|\r")]
    private static partial Regex LineBreakRx();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultiSpaceRx();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiNlRx();
}

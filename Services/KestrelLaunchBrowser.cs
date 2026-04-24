#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CascadeIDE.Services;

/// <summary>Открытие URL после старта Kestrel: <c>launchUrl</c> (как в VS) или первый <c>http(s)</c> из <c>ASPNETCORE_URLS</c>.</summary>
public static class KestrelLaunchBrowser
{
    /// <summary>Устаревшее имя: то же, что <see cref="TryOpenAfterLaunch"/> без <paramref name="launchUrl"/>.</summary>
    public static void TryOpenFirstUrl(IReadOnlyDictionary<string, string>? environment) =>
        TryOpenAfterLaunch(environment, launchUrl: null);

    /// <param name="launchUrl">Из профиля: полный <c>http(s)://…</c> или путь вроде <c>/graphql</c> / <c>graphql</c> (к базе из <c>ASPNETCORE_URLS</c>).</param>
    public static void TryOpenAfterLaunch(IReadOnlyDictionary<string, string>? environment, string? launchUrl)
    {
        var u = ResolveUrlToOpen(environment, launchUrl);
        if (string.IsNullOrEmpty(u))
            return;
        TryStartBrowser(u);
    }

    /// <summary>Разрешить URL для открытия (тесты и прозрачность).</summary>
    public static string? ResolveUrlToOpen(IReadOnlyDictionary<string, string>? environment, string? launchUrl)
    {
        if (string.IsNullOrWhiteSpace(launchUrl))
            return GetFirstHttpOrHttpsFromApplicationUrls(environment);

        var trimmed = launchUrl.Trim();

        if (TryNormalizeAbsoluteHttpUrl(trimmed, out var absolute))
            return absolute;

        var baseFromEnv = GetFirstHttpOrHttpsFromApplicationUrls(environment);
        if (string.IsNullOrEmpty(baseFromEnv))
            return null;

        return CombineBaseWithPath(baseFromEnv, trimmed);
    }

    private static void TryStartBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private static bool TryNormalizeAbsoluteHttpUrl(string value, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;
        if (!IsHttpOrHttpsScheme(uri.Scheme))
            return false;
        normalized = uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
        return true;
    }

    private static bool IsHttpOrHttpsScheme(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static string? CombineBaseWithPath(string baseUrl, string pathOrSegment)
    {
        try
        {
            if (string.IsNullOrEmpty(pathOrSegment))
                return baseUrl;

            var builder = new UriBuilder(baseUrl);

            if (pathOrSegment.StartsWith('/'))
                ApplyRootRelativePath(builder, pathOrSegment);
            else
                AppendPathSegment(builder, pathOrSegment);

            return builder.Uri.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Путь от корня сайта, например <c>/graphql</c> или <c>/g?q=1</c>.</summary>
    private static void ApplyRootRelativePath(UriBuilder builder, string pathOrSegment)
    {
        var q = pathOrSegment.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            builder.Path = pathOrSegment[..q];
            builder.Query = pathOrSegment[(q + 1)..];
        }
        else
            builder.Path = pathOrSegment;
    }

    /// <summary>Сегмент без ведущего <c>/</c> — дописывается к текущему <see cref="UriBuilder.Path"/>.</summary>
    private static void AppendPathSegment(UriBuilder builder, string segment)
    {
        var head = builder.Path;
        var tail = segment.TrimStart('/');
        if (string.IsNullOrEmpty(head) || head == "/")
            builder.Path = "/" + tail;
        else
            builder.Path = head.TrimEnd('/') + "/" + tail;
    }

    private static string? GetFirstHttpOrHttpsFromApplicationUrls(IReadOnlyDictionary<string, string>? environment)
    {
        if (!TryGetAspNetCoreUrlsRaw(environment, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;
        return GetFirstHttpListenUrl(raw);
    }

    /// <summary>Как <see cref="IReadOnlyDictionary{TKey, TValue}.TryGetValue"/>, но false для null/пустой/пробельной строки.</summary>
    private static bool TryGetNonWhiteSpace(
        IReadOnlyDictionary<string, string> environment,
        string key,
        [NotNullWhen(true)] out string? value)
    {
        if (environment.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
        {
            value = v;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetAspNetCoreUrlsRaw(
        IReadOnlyDictionary<string, string>? environment,
        [NotNullWhen(true)] out string? raw)
    {
        raw = null;
        if (environment is null || environment.Count == 0)
            return false;
        if (TryGetNonWhiteSpace(environment, "ASPNETCORE_URLS", out raw))
            return true;
        if (TryGetNonWhiteSpace(environment, "ASPNETCORE__URLS", out raw))
            return true;
        return false;
    }

    private static string? GetFirstHttpListenUrl(string aspNetCoreUrls) =>
        aspNetCoreUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                  u.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
}

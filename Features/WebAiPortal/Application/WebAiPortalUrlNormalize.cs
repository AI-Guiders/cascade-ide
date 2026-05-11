using System.Net;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>Нормализация строки адреса для встроенного WebView: без схемы подставляется <c>https://</c>, для loopback — <c>http://</c>.</summary>
public static class WebAiPortalUrlNormalize
{
    /// <summary>Построить абсолютный URI для веб-навигации и каноническую строку для поля URL.</summary>
    public static bool TryBuildNavigationUri(string? raw, out Uri? uri, out string normalizedText)
    {
        uri = null;
        normalizedText = "";

        var s = raw?.Trim() ?? "";
        if (s.Length == 0)
        {
            uri = new Uri("about:blank");
            normalizedText = "about:blank";
            return true;
        }

        // «//host/path» без схемы .NET часто превращает в file: — обрабатываем до общего TryCreate.
        if (s.StartsWith("//", StringComparison.Ordinal))
        {
            var schemeRelative = "https:" + s;
            return Uri.TryCreate(schemeRelative, UriKind.Absolute, out var abs) &&
                   IsHttpFamily(abs.Scheme) &&
                   AssignResult(abs, out uri, out normalizedText);
        }

        if (TryParseBrowserAbsoluteUri(s, out var absolute))
            return AssignResult(absolute, out uri, out normalizedText);

        string[] prefixes = PreferHttpScheme(s)
            ? ["http://", "https://"]
            : ["https://", "http://"];
        foreach (var prefix in prefixes)
        {
            var candidate = prefix + TrimLeadingAuthoritySlashes(s);
            if (Uri.TryCreate(candidate, UriKind.Absolute, out absolute) && IsHttpFamily(absolute.Scheme))
                return AssignResult(absolute, out uri, out normalizedText);
        }

        return false;
    }

    /// <summary>
    /// Абсолютный URI, который можно открыть во встроенном браузере без доработки строки.
    /// «localhost:5000» и прочие записи без «://» здесь не считаются валидными (пойдут в ветку автопрефикса).
    /// </summary>
    private static bool TryParseBrowserAbsoluteUri(string s, out Uri absolute)
    {
        if (!Uri.TryCreate(s, UriKind.Absolute, out absolute!))
            return false;

        switch (absolute.Scheme)
        {
            case "http":
            case "https":
            case "about":
            case "file":
                return true;
            default:
                // vscode:, mailto:, ftp:… — только если пользователь явно указал схему.
                return s.Contains("://", StringComparison.Ordinal);
        }
    }

    private static bool AssignResult(Uri absolute, out Uri? uri, out string normalizedText)
    {
        uri = absolute;
        normalizedText = absolute.AbsoluteUri;
        return true;
    }

    private static bool IsHttpFamily(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    /// <summary>Loopback без схемы — чаще <c>http</c> (локальные dev-серверы).</summary>
    private static bool PreferHttpScheme(string userInputTrimmed)
    {
        var authority = TrimLeadingAuthoritySlashes(userInputTrimmed);
        var slashIdx = authority.IndexOf('/');
        if (slashIdx >= 0)
            authority = authority[..slashIdx];

        if (authority.StartsWith("[::1]", StringComparison.OrdinalIgnoreCase))
            return true;
        if (authority.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        if (authority.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
            return true;

        var lastColon = authority.LastIndexOf(':');
        if (lastColon > 0)
        {
            var hostPart = authority[..lastColon];
            if (IPAddress.TryParse(hostPart, out var addr) && IPAddress.IsLoopback(addr))
                return true;
        }
        else if (IPAddress.TryParse(authority, out var addr) && IPAddress.IsLoopback(addr))
            return true;

        return false;
    }

    private static string TrimLeadingAuthoritySlashes(string s) =>
        s.StartsWith('/') ? s.TrimStart('/') : s;
}

#nullable enable

namespace CascadeIDE.Features.MagicLink;

public enum CideMagicLinkAction
{
    Reveal,
    Open,
    Markdown,
}

/// <summary>Разобранный <c>cide://</c> URI (ADR 0157).</summary>
public sealed record CideMagicLinkRequest(
    CideMagicLinkAction Action,
    string? WorkspaceRoot,
    string? File,
    int? LineStart,
    int? LineEnd,
    string? BracketInner,
    string? SolutionPath,
    string? DocPath,
    int? DocLine);

public static class CideMagicLinkUri
{
    public const string Scheme = "cide";

    public static bool TryParse(string? raw, out CideMagicLinkRequest request, out string error)
    {
        request = null!;
        error = "";

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Пустой URI.";
            return false;
        }

        var trimmed = raw.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            error = "Ожидается абсолютный cide:// URI.";
            return false;
        }

        var action = ParseAction(uri);
        if (action is null)
        {
            error = $"Неизвестная команда «{uri.Host}» (ожидается reveal, open, md).";
            return false;
        }

        var query = ParseQuery(uri.Query);
        var root = NormalizeOptionalPath(Get(query, "root"));
        var file = NormalizeRepoRelative(Get(query, "f"));
        var bracket = Get(query, "b");
        var lineStart = ParsePositiveInt(Get(query, "l"));
        var lineEnd = ParsePositiveInt(Get(query, "le")) ?? lineStart;
        var sln = NormalizeOptionalPath(Get(query, "sln"));
        if (sln is null)
            sln = NormalizeRepoRelative(Get(query, "sln"));
        var doc = NormalizeRepoRelative(Get(query, "doc"));
        var docLine = ParsePositiveInt(Get(query, "line"));

        if (action == CideMagicLinkAction.Markdown)
        {
            docLine = ParsePositiveInt(Get(query, "l")) ?? docLine;
            if (string.IsNullOrWhiteSpace(doc))
            {
                error = "md: обязателен параметр doc.";
                return false;
            }
        }
        else if (action == CideMagicLinkAction.Reveal)
        {
            if (string.IsNullOrWhiteSpace(file) && string.IsNullOrWhiteSpace(bracket))
            {
                error = "reveal: нужен f или b.";
                return false;
            }
        }

        request = new CideMagicLinkRequest(
            action.Value,
            root,
            file,
            lineStart,
            lineEnd,
            bracket,
            sln,
            doc,
            docLine);
        return true;
    }

    private static CideMagicLinkAction? ParseAction(Uri uri)
    {
        var host = uri.Host;
        if (string.IsNullOrWhiteSpace(host) && uri.AbsolutePath.Length > 1)
            host = uri.AbsolutePath.TrimStart('/').Split('/')[0];

        return host.ToLowerInvariant() switch
        {
            "reveal" => CideMagicLinkAction.Reveal,
            "open" => CideMagicLinkAction.Open,
            "md" or "markdown" or "preview" => CideMagicLinkAction.Markdown,
            _ => null,
        };
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return result;

        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq < 0)
            {
                result[Uri.UnescapeDataString(part)] = "";
                continue;
            }

            var key = Uri.UnescapeDataString(part[..eq]);
            var value = Uri.UnescapeDataString(part[(eq + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private static string? Get(IReadOnlyDictionary<string, string> query, string key) =>
        query.TryGetValue(key, out var value) ? value : null;

    private static string? NormalizeOptionalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return Path.GetFullPath(value.Trim());
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeRepoRelative(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().Replace('\\', '/');
    }

    private static int? ParsePositiveInt(string? value) =>
        int.TryParse(value, out var n) && n > 0 ? n : null;
}

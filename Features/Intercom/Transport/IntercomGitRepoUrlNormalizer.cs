namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Нормализация git remote URL (ADR 0144 §2.3.1).</summary>
public static class IntercomGitRepoUrlNormalizer
{
    public static string? TryNormalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var url = raw.Trim();
        if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            url = url[..^4];

        if (url.StartsWith("git@", StringComparison.Ordinal))
        {
            var at = url.IndexOf('@');
            var colon = url.IndexOf(':', at + 1);
            if (colon < 0)
                return null;
            var sshHost = url[(at + 1)..colon].ToLowerInvariant();
            var sshPath = url[(colon + 1)..].Trim('/');
            return $"{sshHost}/{sshPath.ToLowerInvariant()}";
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var slash = url.IndexOf('/');
            if (slash <= 0)
                return null;
            var bareHost = url[..slash].ToLowerInvariant();
            var barePath = url[(slash + 1)..].Trim('/');
            if (string.IsNullOrWhiteSpace(barePath))
                return null;
            return $"{bareHost}/{barePath.ToLowerInvariant()}";
        }

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
            return null;

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return $"{host}/{path.ToLowerInvariant()}";
    }
}

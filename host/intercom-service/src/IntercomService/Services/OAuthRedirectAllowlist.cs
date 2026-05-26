namespace IntercomService.Services;

/// <summary>Allowlist loopback redirect_uri для OAuth (ADR 0144 §8).</summary>
public static class OAuthRedirectAllowlist
{
    public static bool IsAllowed(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return false;

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!uri.IsLoopback)
            return false;

        return uri.Port is > 0 and < 65536;
    }
}

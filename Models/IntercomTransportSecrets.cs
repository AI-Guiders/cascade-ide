namespace CascadeIDE.Models;

/// <summary>OAuth tokens Intercom transport. Файл: <c>%LocalAppData%\CascadeIDE\intercom-transport-secrets.toml</c> (ADR 0028).</summary>
public sealed class IntercomTransportSecrets
{
    public string AccessToken { get; set; } = "";

    public string RefreshToken { get; set; } = "";

    /// <summary>UTC expiry access token (ISO 8601 round-trip).</summary>
    public string AccessExpiresAtUtc { get; set; } = "";

    public string MemberId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public bool HasRefreshToken => !string.IsNullOrWhiteSpace(RefreshToken);

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

    public DateTimeOffset? TryGetAccessExpiresAtUtc()
    {
        if (string.IsNullOrWhiteSpace(AccessExpiresAtUtc))
            return null;
        return DateTimeOffset.TryParse(AccessExpiresAtUtc, out var at) ? at : null;
    }

    public void SetAccessExpiry(DateTimeOffset expiresAtUtc) =>
        AccessExpiresAtUtc = expiresAtUtc.ToString("O");

    public void ClearTokens()
    {
        AccessToken = "";
        RefreshToken = "";
        AccessExpiresAtUtc = "";
        MemberId = "";
        DisplayName = "";
    }
}

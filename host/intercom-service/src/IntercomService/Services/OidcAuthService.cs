using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntercomService.Data;
using IntercomService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntercomService.Services;

public sealed class OidcAuthService(
    IntercomDbContext db,
    IHttpClientFactory httpClientFactory,
    IOptions<OidcAuthOptions> oidcOptions,
    IConfiguration configuration)
{
    private readonly OidcAuthOptions _oidc = oidcOptions.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_oidc.Authority)
        && !string.IsNullOrWhiteSpace(_oidc.ClientId)
        && !string.IsNullOrWhiteSpace(_oidc.ClientSecret);

    public async Task<OAuthStateEntity> CreateStateAsync(
        string teamId,
        string redirectUri,
        string? codeChallenge,
        string? codeChallengeMethod,
        CancellationToken ct)
    {
        var state = new OAuthStateEntity
        {
            State = Guid.NewGuid().ToString("N"),
            Provider = "oidc",
            TeamId = teamId,
            RedirectUri = redirectUri,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        };
        db.OAuthStates.Add(state);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return state;
    }

    public async Task<OAuthStateEntity?> ConsumeStateAsync(string state, CancellationToken ct)
    {
        var row = await db.OAuthStates.FindAsync([state], ct).ConfigureAwait(false);
        if (row is null || row.ExpiresAtUtc < DateTimeOffset.UtcNow)
            return null;
        db.OAuthStates.Remove(row);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return row;
    }

    public string BuildAuthorizeUrl(OAuthStateEntity state)
    {
        var meta = GetMetadata();
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _oidc.ClientId,
            ["redirect_uri"] = GetCallbackUrl(),
            ["response_type"] = "code",
            ["scope"] = _oidc.Scopes,
            ["state"] = state.State,
        };

        if (!string.IsNullOrWhiteSpace(state.CodeChallenge))
        {
            query["code_challenge"] = state.CodeChallenge;
            query["code_challenge_method"] = state.CodeChallengeMethod ?? "S256";
        }

        var qs = string.Join("&", query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        return $"{meta.AuthorizationEndpoint}?{qs}";
    }

    public async Task<OidcUser?> ExchangeCodeAsync(string code, OAuthStateEntity state, CancellationToken ct)
    {
        var meta = GetMetadata();
        var client = httpClientFactory.CreateClient();

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, meta.TokenEndpoint);
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = GetCallbackUrl(),
            ["client_id"] = _oidc.ClientId,
            ["client_secret"] = _oidc.ClientSecret,
        });

        using var tokenResponse = await client.SendAsync(tokenRequest, ct).ConfigureAwait(false);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var token = JsonSerializer.Deserialize<OidcTokenResponse>(tokenJson, IntercomService.Contracts.IntercomJson.Web);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
            return null;

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, meta.UserInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var userInfoResponse = await client.SendAsync(userInfoRequest, ct).ConfigureAwait(false);
        userInfoResponse.EnsureSuccessStatusCode();
        var userJson = await userInfoResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(userJson);
        var root = doc.RootElement;
        var sub = root.TryGetProperty("sub", out var subEl) ? subEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(sub))
            return null;

        var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString()
            : root.TryGetProperty("preferred_username", out var pu) ? pu.GetString()
            : sub;

        return new OidcUser(_oidc.Authority.TrimEnd('/'), sub, name ?? sub);
    }

    private string GetCallbackUrl()
    {
        var baseUrl = configuration["Intercom:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "http://127.0.0.1:5080";
        return $"{baseUrl}/api/v1/auth/callback/oidc";
    }

    private OidcMetadata GetMetadata()
    {
        var authority = _oidc.Authority.TrimEnd('/');
        return new OidcMetadata(
            $"{authority}/connect/authorize",
            $"{authority}/connect/token",
            $"{authority}/connect/userinfo");
    }

    private sealed record OidcMetadata(string AuthorizationEndpoint, string TokenEndpoint, string UserInfoEndpoint);

    private sealed record OidcTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);

    public sealed record OidcUser(string Issuer, string Subject, string DisplayName);
}

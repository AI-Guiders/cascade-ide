using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>HTTP-клиент reference Intercom service (ADR 0144).</summary>
public sealed class IntercomTransportApiClient : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };

    public void ConfigureBaseUrl(string baseUrl)
    {
        var trimmed = baseUrl.Trim().TrimEnd('/');
        _http.BaseAddress = new Uri(trimmed + "/");
    }

    public async Task<IReadOnlyList<IntercomTopicDto>> ListTopicsAsync(
        string teamId,
        string bearerToken,
        CancellationToken ct)
    {
        using var req = Authorized(HttpMethod.Get, $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/topics", bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<IntercomTopicDto>>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<IntercomTransportEventEnvelopeDto?> AppendEventAsync(
        string topicId,
        IntercomAppendEventRequestDto body,
        string bearerToken,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body, IntercomTransportJson.Web);
        using var req = Authorized(
            HttpMethod.Post,
            $"/api/v1/topics/{Uri.EscapeDataString(topicId)}/events",
            bearerToken);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomTransportEventEnvelopeDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IntercomTransportEventEnvelopeDto>> ListEventsAsync(
        string topicId,
        long afterSeq,
        string bearerToken,
        CancellationToken ct)
    {
        var path = $"/api/v1/topics/{Uri.EscapeDataString(topicId)}/events?after_seq={afterSeq}&limit=200";
        using var req = Authorized(HttpMethod.Get, path, bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<IntercomTransportEventEnvelopeDto>>(
                IntercomTransportJson.Web,
                ct)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<IReadOnlyList<IntercomTransportEventEnvelopeDto>> ListTeamEventsAsync(
        string teamId,
        long afterSeq,
        string bearerToken,
        CancellationToken ct)
    {
        var path = $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/events?after_seq={afterSeq}&limit=500";
        using var req = Authorized(HttpMethod.Get, path, bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<IntercomTransportEventEnvelopeDto>>(
                IntercomTransportJson.Web,
                ct)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<IntercomTopicDto?> EnsureTopicAsync(
        string teamId,
        string spineKey,
        string title,
        string bearerToken,
        CancellationToken ct)
    {
        var body = new IntercomEnsureTopicRequestDto(spineKey, title);
        using var req = Authorized(
            HttpMethod.Post,
            $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/topics/ensure",
            bearerToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomTopicDto>(IntercomTransportJson.Web, ct).ConfigureAwait(false);
    }

    public async Task<IntercomTokenResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var body = new OAuthTokenRequest("refresh_token", refreshToken, null, null, null, null);
        return await postTokenAsync(body, ct).ConfigureAwait(false);
    }

    public async Task<IntercomTokenResponseDto?> ExchangeAuthorizationCodeAsync(
        string code,
        string state,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct)
    {
        var body = new OAuthTokenRequest(
            "authorization_code",
            null,
            code,
            state,
            codeVerifier,
            redirectUri);
        return await postTokenAsync(body, ct).ConfigureAwait(false);
    }

    private async Task<IntercomTokenResponseDto?> postTokenAsync(OAuthTokenRequest body, CancellationToken ct)
    {
        using var res = await _http.PostAsJsonAsync("/api/v1/auth/token", body, IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomTokenResponseDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task LogoutAsync(string refreshToken, string accessToken, CancellationToken ct)
    {
        var body = new { grant_type = "refresh_token", refresh_token = refreshToken };
        using var req = Authorized(HttpMethod.Post, "/api/v1/auth/logout", accessToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        _ = res.StatusCode;
    }

    public async Task<IntercomMeResponseDto?> GetMeAsync(string bearerToken, CancellationToken ct)
    {
        using var req = Authorized(HttpMethod.Get, "/api/v1/auth/me", bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomMeResponseDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IntercomAuthProviderDto>> ListAuthProvidersAsync(CancellationToken ct)
    {
        using var res = await _http.GetAsync("/api/v1/auth/providers", ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return [];
        var list = await res.Content.ReadFromJsonAsync<List<IntercomAuthProviderDto>>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<IntercomWorkspaceContextResponseDto?> ResolveWorkspaceContextAsync(
        string normalizedRepoUrl,
        string bearerToken,
        CancellationToken ct)
    {
        var path = $"/api/v1/resolve/workspace-context?repo_url={Uri.EscapeDataString(normalizedRepoUrl)}";
        using var req = Authorized(HttpMethod.Get, path, bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomWorkspaceContextResponseDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public HttpRequestMessage CreateSseRequest(string teamId, string? topicId, string bearerToken)
    {
        var path = $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/stream";
        if (!string.IsNullOrWhiteSpace(topicId))
            path += $"?topic_id={Uri.EscapeDataString(topicId)}";
        return Authorized(HttpMethod.Get, path, bearerToken);
    }

    public Task<HttpResponseMessage> SendSseAsync(HttpRequestMessage request, CancellationToken ct) =>
        _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

    public static void ApplyTokenResponse(IntercomTransportSecrets secrets, IntercomTokenResponseDto tokens)
    {
        secrets.AccessToken = tokens.AccessToken;
        secrets.RefreshToken = tokens.RefreshToken;
        secrets.SetAccessExpiry(DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, tokens.ExpiresIn - 30)));
        IntercomTransportSecretsStorage.Save(secrets);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string relativePath, string bearerToken)
    {
        var req = new HttpRequestMessage(method, relativePath);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return req;
    }

    public void Dispose() => _http.Dispose();
}

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntercomService.Data;
using IntercomService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntercomService.Services;

public sealed class GitHubAuthService(
    IntercomDbContext db,
    IHttpClientFactory httpClientFactory,
    IOptions<GitHubAuthOptions> githubOptions,
    IConfiguration configuration)
{
    private readonly GitHubAuthOptions _github = githubOptions.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_github.ClientId) && !string.IsNullOrWhiteSpace(_github.ClientSecret);

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
            Provider = "github",
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
        var callback = GetCallbackUrl();
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _github.ClientId,
            ["redirect_uri"] = callback,
            ["scope"] = "read:user",
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

        return $"https://github.com/login/oauth/authorize?{qs}";
    }

    public async Task<GitHubUser?> ExchangeCodeAsync(string code, OAuthStateEntity state, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var callback = GetCallbackUrl();

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _github.ClientId,
            ["client_secret"] = _github.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = callback,
        });

        using var tokenResponse = await client.SendAsync(tokenRequest, ct).ConfigureAwait(false);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var token = JsonSerializer.Deserialize<GitHubTokenResponse>(tokenJson, IntercomService.Contracts.IntercomJson.Web);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
            return null;

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        userRequest.Headers.UserAgent.ParseAdd("IntercomService/1.0");

        using var userResponse = await client.SendAsync(userRequest, ct).ConfigureAwait(false);
        userResponse.EnsureSuccessStatusCode();
        var userJson = await userResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<GitHubUser>(userJson, IntercomService.Contracts.IntercomJson.Web);
    }

    private string GetCallbackUrl()
    {
        var baseUrl = configuration["Intercom:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "http://127.0.0.1:5080";
        return $"{baseUrl}/api/v1/auth/callback/github";
    }

    private sealed record GitHubTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);

    public sealed record GitHubUser(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login);
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Features.Intercom.Transport;

public sealed partial class IntercomTransportApiClient
{
    public async Task<bool> PingHealthAsync(CancellationToken ct = default)
    {
        using var res = await _http.GetAsync("/health", ct).ConfigureAwait(false);
        return res.IsSuccessStatusCode;
    }

    public async Task<IntercomAdminHealthDto?> GetTeamAdminHealthAsync(
        string teamId,
        string bearerToken,
        CancellationToken ct)
    {
        using var req = Authorized(HttpMethod.Get, $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/admin/health", bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomAdminHealthDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IntercomTeamMemberDto>> ListTeamMembersAsync(
        string teamId,
        string bearerToken,
        CancellationToken ct)
    {
        using var req = Authorized(HttpMethod.Get, $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/members", bearerToken);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return [];
        var list = await res.Content.ReadFromJsonAsync<List<IntercomTeamMemberDto>>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<IntercomAgentProvisionDto?> ProvisionAgentAsync(
        string teamId,
        string displayName,
        string? avatarGlyph,
        string bearerToken,
        CancellationToken ct)
    {
        var body = new { display_name = displayName, avatar_glyph = avatarGlyph };
        using var req = Authorized(HttpMethod.Post, $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/agents", bearerToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomAgentProvisionDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task<IntercomInviteDto?> CreateInviteAsync(
        string teamId,
        string teamRole,
        string bearerToken,
        CancellationToken ct)
    {
        var body = new { team_role = teamRole, ttl_hours = 168, max_uses = 1 };
        using var req = Authorized(HttpMethod.Post, $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/invites", bearerToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomInviteDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task<IntercomProjectDto?> CreateProjectAsync(
        string projectId,
        string? displayName,
        string bearerToken,
        CancellationToken ct)
    {
        var body = new { project_id = projectId, display_name = displayName };
        using var req = Authorized(HttpMethod.Post, "/api/v1/projects", bearerToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            return null;
        return await res.Content.ReadFromJsonAsync<IntercomProjectDto>(IntercomTransportJson.Web, ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> PutProjectReposAsync(
        string projectId,
        IReadOnlyList<string> repoUrls,
        string bearerToken,
        CancellationToken ct)
    {
        var body = new { repo_urls = repoUrls };
        using var req = Authorized(HttpMethod.Put, $"/api/v1/projects/{Uri.EscapeDataString(projectId)}/repos", bearerToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> PutTeamProjectsAsync(
        string teamId,
        IReadOnlyList<string> projectIds,
        string bearerToken,
        CancellationToken ct)
    {
        var body = new { project_ids = projectIds };
        using var req = Authorized(HttpMethod.Put, $"/api/v1/teams/{Uri.EscapeDataString(teamId)}/projects", bearerToken);
        req.Content = JsonContent.Create(body, options: IntercomTransportJson.Web);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        return res.IsSuccessStatusCode;
    }
}

public sealed record IntercomAdminHealthDto(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("sse_subscribers")] int SseSubscribers);

public sealed record IntercomTeamMemberDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("member_kind")] string MemberKind,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("team_display_name")] string? TeamDisplayName,
    [property: JsonPropertyName("joined_at_utc")] string JoinedAtUtc);

public sealed record IntercomAgentProvisionDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_glyph")] string? AvatarGlyph,
    [property: JsonPropertyName("credential_token")] string CredentialToken);

public sealed record IntercomInviteDto(
    [property: JsonPropertyName("invite_id")] string InviteId,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("expires_at_utc")] string ExpiresAtUtc,
    [property: JsonPropertyName("max_uses")] int MaxUses);

public sealed record IntercomProjectDto(
    [property: JsonPropertyName("project_id")] string ProjectId,
    [property: JsonPropertyName("display_name")] string DisplayName);

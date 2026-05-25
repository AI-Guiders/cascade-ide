using System.Text.Json.Serialization;

namespace IntercomService.Contracts;

public sealed record MeTeamDto(
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("team_display_name")] string? TeamDisplayName = null);

public sealed record MeResponse(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("member_kind")] string MemberKind,
    [property: JsonPropertyName("teams")] IReadOnlyList<MeTeamDto> Teams);

public sealed record PatchMeRequest(
    [property: JsonPropertyName("display_name")] string? DisplayName);

public sealed record AuthProviderDto(
    [property: JsonPropertyName("provider_id")] string ProviderId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("enabled")] bool Enabled);

public sealed record CreateTeamRequest(
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("display_name")] string? DisplayName);

public sealed record PatchTeamRequest(
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("join_policy")] string? JoinPolicy,
    [property: JsonPropertyName("default_team_role")] string? DefaultTeamRole,
    [property: JsonPropertyName("join_policy_json")] string? JoinPolicyJson);

public sealed record TeamDto(
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("join_policy")] string JoinPolicy,
    [property: JsonPropertyName("default_team_role")] string DefaultTeamRole);

public sealed record TeamMemberDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("member_kind")] string MemberKind,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("team_display_name")] string? TeamDisplayName,
    [property: JsonPropertyName("joined_at_utc")] string JoinedAtUtc);

public sealed record PatchTeamMemberRequest(
    [property: JsonPropertyName("team_role")] string? TeamRole,
    [property: JsonPropertyName("team_display_name")] string? TeamDisplayName,
    [property: JsonPropertyName("clear_team_display_name")] bool ClearTeamDisplayName = false);

public sealed record PatchSelfTeamMemberRequest(
    [property: JsonPropertyName("team_display_name")] string? TeamDisplayName,
    [property: JsonPropertyName("clear_team_display_name")] bool ClearTeamDisplayName = false);

public sealed record CreateInviteRequest(
    [property: JsonPropertyName("team_role")] string? TeamRole,
    [property: JsonPropertyName("ttl_hours")] int? TtlHours,
    [property: JsonPropertyName("max_uses")] int? MaxUses);

public sealed record InviteDto(
    [property: JsonPropertyName("invite_id")] string InviteId,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("expires_at_utc")] string ExpiresAtUtc,
    [property: JsonPropertyName("max_uses")] int MaxUses);

public sealed record RedeemInviteRequest(
    [property: JsonPropertyName("invite_token")] string InviteToken,
    [property: JsonPropertyName("team_id")] string TeamId);

public sealed record CreateAgentRequest(
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_glyph")] string? AvatarGlyph);

public sealed record AgentDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_glyph")] string? AvatarGlyph,
    [property: JsonPropertyName("credential_token")] string CredentialToken);

public sealed record PatchAgentRequest(
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("avatar_glyph")] string? AvatarGlyph,
    [property: JsonPropertyName("revoke_credentials")] bool RevokeCredentials = false);

public sealed record CreateProjectRequest(
    [property: JsonPropertyName("project_id")] string ProjectId,
    [property: JsonPropertyName("display_name")] string? DisplayName);

public sealed record ProjectDto(
    [property: JsonPropertyName("project_id")] string ProjectId,
    [property: JsonPropertyName("display_name")] string DisplayName);

public sealed record PutProjectReposRequest(
    [property: JsonPropertyName("repo_urls")] IReadOnlyList<string> RepoUrls);

public sealed record PutTeamProjectsRequest(
    [property: JsonPropertyName("project_ids")] IReadOnlyList<string> ProjectIds);

public sealed record WorkspaceContextProjectDto(
    [property: JsonPropertyName("project_id")] string ProjectId,
    [property: JsonPropertyName("display_name")] string DisplayName);

public sealed record WorkspaceContextTeamDto(
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("project_id")] string ProjectId);

public sealed record WorkspaceContextResponse(
    [property: JsonPropertyName("normalized_repo_urls")] IReadOnlyList<string> NormalizedRepoUrls,
    [property: JsonPropertyName("projects")] IReadOnlyList<WorkspaceContextProjectDto> Projects,
    [property: JsonPropertyName("teams")] IReadOnlyList<WorkspaceContextTeamDto> Teams,
    [property: JsonPropertyName("suggested_team_id")] string? SuggestedTeamId);

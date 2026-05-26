using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Features.Intercom.Transport;

public sealed record IntercomSenderWireDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("sender_role")] string SenderRole,
    [property: JsonPropertyName("client_kind")] string ClientKind);

public sealed record IntercomTransportEventEnvelopeDto(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("seq")] long Seq,
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("topic_id")] string TopicId,
    [property: JsonPropertyName("client_event_id")] string ClientEventId,
    [property: JsonPropertyName("occurred_at_utc")] string OccurredAtUtc,
    [property: JsonPropertyName("event_kind")] string EventKind,
    [property: JsonPropertyName("sender")] IntercomSenderWireDto Sender,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public sealed record IntercomAppendEventRequestDto(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("client_event_id")] string ClientEventId,
    [property: JsonPropertyName("occurred_at_utc")] string? OccurredAtUtc,
    [property: JsonPropertyName("event_kind")] string EventKind,
    [property: JsonPropertyName("sender")] IntercomSenderWireDto? Sender,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public sealed record IntercomTopicDto(
    [property: JsonPropertyName("topic_id")] string TopicId,
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("spine_key")] string? SpineKey);

public sealed record IntercomTokenResponseDto(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType);

public sealed record OAuthTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("code_verifier")] string? CodeVerifier,
    [property: JsonPropertyName("redirect_uri")] string? RedirectUri);

public sealed record IntercomMeTeamDto(
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("team_display_name")] string? TeamDisplayName = null);

public sealed record IntercomMeResponseDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("member_kind")] string MemberKind,
    [property: JsonPropertyName("teams")] IReadOnlyList<IntercomMeTeamDto> Teams);

public sealed record IntercomAuthProviderDto(
    [property: JsonPropertyName("provider_id")] string ProviderId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("enabled")] bool Enabled);

public sealed record IntercomWorkspaceContextTeamDto(
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("team_role")] string TeamRole,
    [property: JsonPropertyName("project_id")] string ProjectId);

public sealed record IntercomWorkspaceContextResponseDto(
    [property: JsonPropertyName("normalized_repo_urls")] IReadOnlyList<string> NormalizedRepoUrls,
    [property: JsonPropertyName("projects")] IReadOnlyList<object> Projects,
    [property: JsonPropertyName("teams")] IReadOnlyList<IntercomWorkspaceContextTeamDto> Teams,
    [property: JsonPropertyName("suggested_team_id")] string? SuggestedTeamId);

public sealed record IntercomEnsureTopicRequestDto(
    [property: JsonPropertyName("spine_key")] string SpineKey,
    [property: JsonPropertyName("title")] string Title);

public static class IntercomTransportJson
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}

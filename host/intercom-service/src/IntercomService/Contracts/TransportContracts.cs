using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntercomService.Contracts;

public sealed record SenderDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("sender_role")] string SenderRole,
    [property: JsonPropertyName("client_kind")] string ClientKind);

public sealed record TransportEventEnvelopeDto(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("seq")] long Seq,
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("topic_id")] string TopicId,
    [property: JsonPropertyName("client_event_id")] string ClientEventId,
    [property: JsonPropertyName("occurred_at_utc")] string OccurredAtUtc,
    [property: JsonPropertyName("event_kind")] string EventKind,
    [property: JsonPropertyName("sender")] SenderDto Sender,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public sealed record AppendEventRequest(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("client_event_id")] string ClientEventId,
    [property: JsonPropertyName("occurred_at_utc")] string? OccurredAtUtc,
    [property: JsonPropertyName("event_kind")] string EventKind,
    [property: JsonPropertyName("sender")] SenderDto? Sender,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public sealed record CreateTopicRequest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("spine_key")] string? SpineKey);

public sealed record EnsureTopicRequest(
    [property: JsonPropertyName("spine_key")] string SpineKey,
    [property: JsonPropertyName("title")] string? Title);

public sealed record TopicDto(
    [property: JsonPropertyName("topic_id")] string TopicId,
    [property: JsonPropertyName("team_id")] string TeamId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("spine_key")] string? SpineKey);

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType = "Bearer");

public sealed record RefreshTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);

public sealed record OAuthTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("code_verifier")] string? CodeVerifier,
    [property: JsonPropertyName("redirect_uri")] string? RedirectUri);

public static class IntercomJson
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}

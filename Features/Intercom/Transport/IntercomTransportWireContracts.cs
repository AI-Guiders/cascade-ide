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

public sealed record IntercomMeResponseDto(
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("teams")] IReadOnlyList<string> Teams);

public sealed record IntercomEnsureTopicRequestDto(
    [property: JsonPropertyName("spine_key")] string SpineKey,
    [property: JsonPropertyName("title")] string Title);

public static class IntercomTransportJson
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}

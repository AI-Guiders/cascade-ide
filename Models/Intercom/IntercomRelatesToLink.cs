#nullable enable

using System.Text.Json.Serialization;

namespace CascadeIDE.Models.Intercom;

/// <summary>Wire extension <c>relates_to</c> v1 (see wire/intercom-wire/schemas/v1/extensions/relates-to-v1).</summary>
public sealed record IntercomRelatesToLink(
    [property: JsonPropertyName("target_kind")] string TargetKind,
    [property: JsonPropertyName("relation")] string Relation,
    [property: JsonPropertyName("code_ref")] AttachmentAnchor? CodeRef = null,
    [property: JsonPropertyName("message_id")] string? MessageId = null,
    [property: JsonPropertyName("doc_path")] string? DocPath = null,
    [property: JsonPropertyName("topic_id")] string? TopicId = null,
    [property: JsonPropertyName("ordinal_range")] IntercomOrdinalRange? OrdinalRange = null,
    [property: JsonPropertyName("confidence")] string? Confidence = null);

public sealed record IntercomOrdinalRange(
    [property: JsonPropertyName("start_ordinal")] int StartOrdinal,
    [property: JsonPropertyName("end_ordinal")] int EndOrdinal);

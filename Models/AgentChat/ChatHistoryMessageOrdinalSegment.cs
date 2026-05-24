using System.Text.Json.Serialization;

namespace CascadeIDE.Models.AgentChat;

/// <summary>Один contiguous сегмент gutter ordinals (ADR 0137 contiguous, 0138 disjoint).</summary>
public sealed record ChatHistoryMessageOrdinalSegment(
    [property: JsonPropertyName("start_ordinal")] int StartOrdinal,
    [property: JsonPropertyName("end_ordinal")] int EndOrdinal);

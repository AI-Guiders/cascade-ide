#nullable enable
using System.Text.Json;

namespace CascadeIDE.Features.Chat;

/// <summary>JSON для append-only истории чата (NDJSON payload + проекции).</summary>
internal static class ChatHistoryJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T payload) =>
        JsonSerializer.Serialize(payload, Options);
}

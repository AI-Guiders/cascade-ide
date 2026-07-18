using System.Text.Json;

namespace CascadeIDE.Services.Fm;

/// <summary>Парсинг <c>usage</c> из OpenAI-compatible chat completion (stream/non-stream).</summary>
public static class FmOpenAiUsageParser
{
    public static FmTurnUsage? TryParseFromCompletionChunk(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryParseFromRoot(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static FmTurnUsage? TryParseFromRoot(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
            return null;

        int? prompt = TryReadInt(usage, "prompt_tokens");
        int? completion = TryReadInt(usage, "completion_tokens");
        int? total = TryReadInt(usage, "total_tokens");
        return FmTurnUsage.TryCreate(prompt, completion, total);
    }

    private static int? TryReadInt(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.Number when el.TryGetInt32(out var n) => n,
            JsonValueKind.Number => (int)el.GetDouble(),
            _ => null,
        };
    }
}

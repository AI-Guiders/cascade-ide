#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>ADR 0096 topic-card summary: meta + truncated last meaningful body (Avalonia-free).</summary>
public static class GlassTopicCardSummary
{
    public const int MaxBodyLen = 160;

    public static string Format(
        int count,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        IEnumerable<string?> bodiesOldestFirst)
    {
        var meta = $"{count} msg · {startUtc.ToLocalTime():HH:mm}–{endUtc.ToLocalTime():HH:mm}";
        var body = Truncate(PickLastMeaningful(bodiesOldestFirst));
        return body.Length == 0 ? meta : meta + "\n" + body;
    }

    public static string Truncate(string? text)
    {
        var normalized = string.Join(
            ' ',
            (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
            return "";
        return normalized.Length <= MaxBodyLen ? normalized : normalized[..MaxBodyLen] + "…";
    }

    static string PickLastMeaningful(IEnumerable<string?> bodiesOldestFirst)
    {
        string? last = null;
        foreach (var body in bodiesOldestFirst)
        {
            if (string.IsNullOrWhiteSpace(body))
                continue;
            last = body;
        }

        return last ?? "";
    }
}

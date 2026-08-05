#nullable enable

using System.Text.Json;

namespace CascadeIDE.Intercom;

/// <summary>
/// NorthStar channel rail (design glass-intercom-northstar-messenger-v0).
/// Kind = #crew | Radio | DM — XOR Korry; orthogonal to lane×model transport.
/// Pure — no WPF.
/// </summary>
public static class GlassIntercomChannel
{
    public const string Schema = "glass_intercom_channel/v0";

    public enum Kind
    {
        Crew,
        Radio,
        Dm
    }

    public readonly record struct Snapshot(Kind Channel);

    public static Kind DefaultKind => Kind.Radio;

    public static string Code(Kind channel) => channel switch
    {
        Kind.Crew => "crew",
        Kind.Radio => "radio",
        Kind.Dm => "dm",
        _ => "radio"
    };

    public static string Label(Kind channel) => channel switch
    {
        Kind.Crew => "#crew",
        Kind.Radio => "Radio",
        Kind.Dm => "DM",
        _ => "Radio"
    };

    public static string Tooltip(Kind channel) => channel switch
    {
        Kind.Crew => "#crew · humans+agents together (NorthStar hub)",
        Kind.Radio => "Radio · operator ↔ this seat / citizen partner",
        Kind.Dm => "DM · 1:1 (address book later)",
        _ => "Radio · operator ↔ this seat / citizen partner"
    };

    /// <summary>Feed filter: missing/blank journal channel = Radio (pre-tag backcompat).</summary>
    public static bool MatchesFeed(Kind active, string? entryChannel) =>
        Parse(entryChannel) == active;

    public static Kind Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultKind;

        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "crew" or "#crew" => Kind.Crew,
            "radio" => Kind.Radio,
            "dm" or "direct" or "1:1" => Kind.Dm,
            _ => DefaultKind
        };
    }

    public static Snapshot ParseLatchJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new Snapshot(DefaultKind);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new Snapshot(DefaultKind);

            if (root.TryGetProperty("channel", out var ch) && ch.ValueKind == JsonValueKind.String)
                return new Snapshot(Parse(ch.GetString()));

            return new Snapshot(DefaultKind);
        }
        catch
        {
            return new Snapshot(DefaultKind);
        }
    }

    public static string FormatLatchJson(Kind channel, DateTimeOffset? stampedUtc = null)
    {
        var stamp = (stampedUtc ?? DateTimeOffset.UtcNow).ToString("o");
        var doc = new Dictionary<string, object?>
        {
            ["schema"] = Schema,
            ["channel"] = Code(channel),
            ["stamped_utc"] = stamp
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}

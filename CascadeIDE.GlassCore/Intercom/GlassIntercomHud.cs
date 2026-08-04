#nullable enable

using System.Text.Json;

namespace CascadeIDE.Intercom;

/// <summary>
/// Intercom HUD state from ignite-LATEST (flat Korry + HDG/CRS). Pure — no WPF.
/// </summary>
public static class GlassIntercomHud
{
    public const string IgniteSchema = "cide_ignite_latch/v1";
    public const int MaxHdgChars = 96;

    public readonly record struct Snapshot(
        bool Autoi,
        bool Hild,
        bool Vad,
        string HdgCrs,
        bool ContinuityActive,
        string? Pulse);

    public static Snapshot Empty { get; } =
        new(false, false, false, "HDG/CRS · —", false, null);

    public static Snapshot ParseIgniteJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Empty;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Empty;

            if (TryGetString(root, "schema") is { } schema
                && !string.Equals(schema, IgniteSchema, StringComparison.OrdinalIgnoreCase))
                return Empty;

            var autoi = TryGetBool(root, "autonomous") ?? TryGetBool(root, "autoi") ?? false;
            var hild = TryGetBool(root, "hild") ?? false;
            var vad = TryGetBool(root, "vad") ?? false;
            var active = TryGetBool(root, "active") ?? false;
            var pulse = TryGetString(root, "pulse") ?? TryGetString(root, "chrome_hint");
            var course = TryGetString(root, "course");
            return new Snapshot(autoi, hild, vad, FormatHdgCrs(course), active, pulse);
        }
        catch
        {
            return Empty;
        }
    }

    public static string FormatHdgCrs(string? courseOrBody)
    {
        var goal = ExtractGoalLine(courseOrBody);
        if (string.IsNullOrWhiteSpace(goal))
            return "HDG/CRS · —";

        if (goal.Length > MaxHdgChars)
            goal = goal[..(MaxHdgChars - 1)].TrimEnd() + "…";

        return "HDG/CRS · " + goal;
    }

    public static string ExtractGoalLine(string? courseOrBody)
    {
        if (string.IsNullOrWhiteSpace(courseOrBody))
            return "";

        foreach (var raw in courseOrBody.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("Before act", StringComparison.OrdinalIgnoreCase))
                break;
            if (line.StartsWith("Empty TM", StringComparison.OrdinalIgnoreCase))
                continue;

            if (line.Length > 2 && char.IsDigit(line[0]))
            {
                var dot = line.IndexOf('.');
                if (dot is > 0 and < 4 && dot + 1 < line.Length)
                    line = line[(dot + 1)..].Trim();
            }

            return line;
        }

        return "";
    }

    public static string ToggleOp(string korry, bool currentlyOn) =>
        (korry.Trim().ToLowerInvariant(), currentlyOn) switch
        {
            ("autoi" or "autonomous", true) => "autonomous_off",
            ("autoi" or "autonomous", false) => "autonomous_on",
            ("hild", true) => "hild_off",
            ("hild", false) => "hild_on",
            _ => ""
        };

    static bool? TryGetBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    static string? TryGetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return null;
        var s = el.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}

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
        string? Pulse,
        bool AwaitPartner,
        string Mode,
        string AutoiLabel);

    public static Snapshot Empty { get; } =
        new(false, false, false, "HDG/CRS · —", false, null, false, "fly", "AUTOI");

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

            var autonomous = TryGetBool(root, "autonomous") ?? TryGetBool(root, "autoi") ?? false;
            var hild = TryGetBool(root, "hild") ?? false;
            var vad = TryGetBool(root, "vad") ?? false;
            var active = TryGetBool(root, "active") ?? false;
            var awaitPartner = TryGetBool(root, "await_partner") ?? false;
            var awaitingCount = TryGetInt(root, "awaiting_count") ?? 0;
            var mode = NormalizeMode(TryGetString(root, "mode"), awaitPartner || awaitingCount > 0);
            if (mode is "talk" or "halt")
                awaitPartner = true;
            // Talk/halt: Autoi Korry OFF even if autonomous latch still true (soft await).
            var autoi = autonomous && !awaitPartner;
            var pulse = TryGetString(root, "pulse") ?? TryGetString(root, "chrome_hint");
            var course = TryGetString(root, "course");
            var hdg = mode is "talk" or "halt"
                ? FormatTalkHdg(mode)
                : FormatHdgCrs(course);
            var label = mode switch
            {
                "talk" => "TALK",
                "halt" => "HALT",
                _ => "AUTOI"
            };
            return new Snapshot(autoi, hild, vad, hdg, active, pulse, awaitPartner, mode, label);
        }
        catch
        {
            return Empty;
        }
    }

    public static string NormalizeMode(string? mode, bool awaitPartner)
    {
        if (!string.IsNullOrWhiteSpace(mode))
        {
            var m = mode.Trim().ToLowerInvariant();
            if (m is "fly" or "talk" or "halt")
                return m;
        }

        return awaitPartner ? "talk" : "fly";
    }

    public static string FormatTalkHdg(string mode) =>
        mode.Equals("halt", StringComparison.OrdinalIgnoreCase)
            ? "HDG/CRS · HALT · Autoi OFF"
            : "HDG/CRS · TALK · Autoi OFF";

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

    static int? TryGetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n) ? n : null;
    }

    static string? TryGetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return null;
        var s = el.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}

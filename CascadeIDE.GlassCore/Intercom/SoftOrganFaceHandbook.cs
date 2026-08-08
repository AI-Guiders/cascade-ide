#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Human SoftOrgan Face — Plan-like glance cards + find, not markdown wall.</summary>
public static class SoftOrganFaceHandbook
{
    public static string MfdPageFor(string organId) =>
        (organId ?? "").Trim().ToLowerInvariant() switch
        {
            "qrh" => "QRH",
            "ecl" => "ECL",
            "alert" or "eicas" or "sa" => "Alert",
            _ => "QRH",
        };

    public static bool IsSoftOrganGlancePage(string? page) =>
        page is "QRH" or "ECL" or "Alert";

    public static string OrganIdFromMfdPage(string? page) =>
        (page ?? "").Trim() switch
        {
            "QRH" => "qrh",
            "ECL" => "ecl",
            "Alert" => "alert",
            _ => "qrh",
        };

    public static IReadOnlyList<GlassGlanceChip> ChipsFor(string organId, string? filter = null)
    {
        var id = (organId ?? "").Trim().ToLowerInvariant();
        IReadOnlyList<GlassGlanceChip> chips = id switch
        {
            "qrh" => QrhChips(),
            "ecl" => EclChips(),
            "alert" or "eicas" or "sa" => AlertChips(),
            _ => [new("LEVEL", "UNKNOWN", "idle")],
        };
        return Filter(chips, filter);
    }

    static IReadOnlyList<GlassGlanceChip> QrhChips() =>
    [
        new("LEVEL", "QRH", "ok"),
        new("FACE", "cards · find", "meta"),
        new("intake-brief", "cold start / remount", "ok"),
        new("path-mutate", "buffer edit gate", "warn"),
        new("dig-before-ask", "habitat dig first", "ok"),
        new("human-face-shot", "#CIDE evidence PNG", "warn"),
        new("softfl-invent", "lived residual only", "meta"),
    ];

    static IReadOnlyList<GlassGlanceChip> EclChips() =>
    [
        new("LEVEL", "ECL", "ok"),
        new("FACE", "cards · find", "meta"),
        new("not-connected", "Recover seat remount", "warn"),
        new("hard-deploy", "terminal · KillRunning", "warn"),
        new("path-mutate", "dig gate · no Write slap", "warn"),
        new("composer-stop", "Voice / Face Radio tip", "meta"),
    ];

    static IReadOnlyList<GlassGlanceChip> AlertChips() =>
    [
        new("LEVEL", "ALERT", "ok"),
        new("FACE", "cards · find", "meta"),
        new("sa-desk", "situation pulse", "ok"),
        new("prefer-surface", "chrome ≠ body", "meta"),
        new("eicas-keys", "clr · ack · list", "idle"),
    ];

    internal static IReadOnlyList<GlassGlanceChip> Filter(IReadOnlyList<GlassGlanceChip> chips, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return chips;

        var q = filter.Trim();
        return chips
            .Where(c =>
                c.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                || c.Value.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// SoftInstrument latch file stems (<c>{id}-LATEST.json</c>) — Glass LatchHub + Avalonia seat ids.
/// Density priority stays in <see cref="SoftInstrumentChromeDensityPolicy"/>.
/// Avalonia seat table must use these ids (canonical <see cref="SaDesk"/>, not <c>sa_desk</c>).
/// </summary>
public static class SoftInstrumentLatchCatalog
{
    /// <summary>Canonical SoftInstrument sa-desk latch stem (<c>sa-desk-LATEST.json</c>).</summary>
    public const string SaDesk = "sa-desk";

    /// <summary>Citizen hands receipt SoftInstrument latch stem (<c>hands-LATEST.json</c>) — Face chips, not letter laundry.</summary>
    public const string Hands = "hands";

    /// <summary>Canonical Glass SoftInstrument latch ids (ordinal-ignore case).</summary>
    public static IReadOnlyCollection<string> Ids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pressure", "ignite", "plan", Hands, "cabin", "scope", "review", "refactor", "plugins",
            "toolchain", "test_desk", "debug_desk", "build_desk", "files_desk", "find_desk", "crm", "report", "webcam", "sys", "onboard", "arch", "mcp", "learn", "domain",
            "md_author", "rules", "calendar", "fdr", "teeth", "postmortem", "glass", "problems",
            SaDesk
        };

    /// <summary>Map legacy Avalonia <c>sa_desk</c> → <see cref="SaDesk"/>; otherwise trim.</summary>
    public static string Canonicalize(string? organId)
    {
        if (string.IsNullOrWhiteSpace(organId))
            return "";
        var id = organId.Trim();
        if (id.Equals("sa_desk", StringComparison.OrdinalIgnoreCase)
            || id.Equals(SaDesk, StringComparison.OrdinalIgnoreCase))
            return SaDesk;
        if (id.Equals("test", StringComparison.OrdinalIgnoreCase)
            || id.Equals("test_sa", StringComparison.OrdinalIgnoreCase)
            || id.Equals("test_desk", StringComparison.OrdinalIgnoreCase))
            return "test_desk";
        if (id.Equals("debug", StringComparison.OrdinalIgnoreCase)
            || id.Equals("dap_sa", StringComparison.OrdinalIgnoreCase)
            || id.Equals("debug_sa", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_debug_sa", StringComparison.OrdinalIgnoreCase)
            || id.Equals("debug_desk", StringComparison.OrdinalIgnoreCase))
            return "debug_desk";
        if (id.Equals("files", StringComparison.OrdinalIgnoreCase)
            || id.Equals("explorer", StringComparison.OrdinalIgnoreCase)
            || id.Equals("fm", StringComparison.OrdinalIgnoreCase)
            || id.Equals("file_manager", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_files", StringComparison.OrdinalIgnoreCase)
            || id.Equals("files_desk", StringComparison.OrdinalIgnoreCase))
            return "files_desk";
        if (id.Equals("find", StringComparison.OrdinalIgnoreCase)
            || id.Equals("search", StringComparison.OrdinalIgnoreCase)
            || id.Equals("search_desk", StringComparison.OrdinalIgnoreCase)
            || id.Equals("code_search", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_search", StringComparison.OrdinalIgnoreCase)
            || id.Equals("find_desk", StringComparison.OrdinalIgnoreCase))
            return "find_desk";
        if (id.Equals("build", StringComparison.OrdinalIgnoreCase)
            || id.Equals("build_sa", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_build_sa", StringComparison.OrdinalIgnoreCase)
            || id.Equals("build_desk", StringComparison.OrdinalIgnoreCase))
            return "build_desk";
        if (id.Equals("project_switch", StringComparison.OrdinalIgnoreCase)
            || id.Equals("ps", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_scope", StringComparison.OrdinalIgnoreCase)
            || id.Equals("scope", StringComparison.OrdinalIgnoreCase))
            return "scope";
        if (id.Equals("surface", StringComparison.OrdinalIgnoreCase)
            || id.Equals("surface_desk", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_glass", StringComparison.OrdinalIgnoreCase)
            || id.Equals("glass", StringComparison.OrdinalIgnoreCase))
            return "glass";
        if (id.Equals("chk", StringComparison.OrdinalIgnoreCase)
            || id.Equals("ecl_organ", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_ecl", StringComparison.OrdinalIgnoreCase)
            || id.Equals("ecl", StringComparison.OrdinalIgnoreCase))
            return "ecl";
        if (id.Equals("eicas", StringComparison.OrdinalIgnoreCase)
            || id.Equals("alert_channel", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_alert", StringComparison.OrdinalIgnoreCase)
            || id.Equals("alert", StringComparison.OrdinalIgnoreCase))
            return "alert";
        if (id.Equals("problems_channel", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_problems", StringComparison.OrdinalIgnoreCase)
            || id.Equals("problems", StringComparison.OrdinalIgnoreCase))
            return "problems";
        if (id.Equals("hand", StringComparison.OrdinalIgnoreCase)
            || id.Equals("receipt", StringComparison.OrdinalIgnoreCase)
            || id.Equals("hands_receipt", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_hands", StringComparison.OrdinalIgnoreCase)
            || id.Equals(Hands, StringComparison.OrdinalIgnoreCase))
            return Hands;
        if (id.Equals("cdp_md_author", StringComparison.OrdinalIgnoreCase)
            || id.Equals("md_author", StringComparison.OrdinalIgnoreCase))
            return "md_author";
        if (id.Equals("cdp_fdr", StringComparison.OrdinalIgnoreCase)
            || id.Equals("fdr", StringComparison.OrdinalIgnoreCase))
            return "fdr";
        if (id.Equals("cdp_teeth", StringComparison.OrdinalIgnoreCase)
            || id.Equals("teeth", StringComparison.OrdinalIgnoreCase))
            return "teeth";
        if (id.Equals("cdp_postmortem", StringComparison.OrdinalIgnoreCase)
            || id.Equals("postmortem", StringComparison.OrdinalIgnoreCase))
            return "postmortem";
        if (id.Equals("cdp_rules", StringComparison.OrdinalIgnoreCase)
            || id.Equals("rules", StringComparison.OrdinalIgnoreCase))
            return "rules";
        if (id.Equals("calendar_desk", StringComparison.OrdinalIgnoreCase)
            || id.Equals("cdp_calendar", StringComparison.OrdinalIgnoreCase)
            || id.Equals("calendar", StringComparison.OrdinalIgnoreCase))
            return "calendar";
        return id;
    }

    public static bool Contains(string? organId)
    {
        var id = Canonicalize(organId);
        return id.Length > 0 && Ids.Contains(id);
    }

    /// <summary>Parse <c>{id}-LATEST.json</c> when id is in the catalog (canonicalized).</summary>
    public static bool TryParseFileName(string fileName, out string organId)
    {
        organId = "";
        const string suffix = "-LATEST.json";
        if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return false;
        var raw = fileName[..^suffix.Length];
        if (!Contains(raw))
            return false;
        organId = Canonicalize(raw);
        return true;
    }
}

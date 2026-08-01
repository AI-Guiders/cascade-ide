#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// SoftOrgan latch file stems (<c>{id}-LATEST.json</c>) — Glass LatchHub + Avalonia seat ids.
/// Density priority stays in <see cref="SoftOrganChromeDensityPolicy"/>.
/// Avalonia seat table must use these ids (canonical <see cref="SaDesk"/>, not <c>sa_desk</c>).
/// </summary>
public static class SoftOrganLatchCatalog
{
    /// <summary>Canonical SoftOrgan sa-desk latch stem (<c>sa-desk-LATEST.json</c>).</summary>
    public const string SaDesk = "sa-desk";

    /// <summary>Canonical Glass SoftOrgan latch ids (ordinal-ignore case).</summary>
    public static IReadOnlyCollection<string> Ids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pressure", "ignite", "plan", "cabin", "scope", "review", "refactor", "plugins",
            "toolchain", "test_desk", "debug_desk", "files_desk", "crm", "report", "webcam", "sys", "onboard", "arch", "mcp", "learn", "domain",
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

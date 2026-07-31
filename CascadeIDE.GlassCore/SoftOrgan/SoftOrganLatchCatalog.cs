#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// SoftOrgan latch file stems (<c>{id}-LATEST.json</c>) recognized by Glass LatchHub.
/// Density priority stays in <see cref="SoftOrganChromeDensityPolicy"/>;
/// Avalonia seats remain ViewModel property table (host-specific).
/// </summary>
public static class SoftOrganLatchCatalog
{
    /// <summary>Canonical Glass SoftOrgan latch ids (ordinal-ignore case).</summary>
    public static IReadOnlyCollection<string> Ids { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pressure", "ignite", "plan", "cabin", "scope", "review", "refactor", "plugins",
            "toolchain", "crm", "report", "webcam", "sys", "onboard", "arch", "mcp", "learn", "domain",
            "sa-desk"
        };

    public static bool Contains(string? organId) =>
        !string.IsNullOrWhiteSpace(organId) && Ids.Contains(organId);

    /// <summary>Parse <c>{id}-LATEST.json</c> when id is in the catalog.</summary>
    public static bool TryParseFileName(string fileName, out string organId)
    {
        organId = "";
        const string suffix = "-LATEST.json";
        if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return false;
        organId = fileName[..^suffix.Length];
        return Contains(organId);
    }
}

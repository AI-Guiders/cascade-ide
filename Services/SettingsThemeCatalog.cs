namespace CascadeIDE.Services;

/// <summary>JSON-темы из <c>Themes/*.json</c> (не settings.toml).</summary>
public static class SettingsThemeCatalog
{
    public sealed record ThemeFileEntry(string DisplayName, string FullPath);

    public static IReadOnlyList<ThemeFileEntry> DiscoverBundled()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Themes");
        if (!Directory.Exists(dir))
            return [];

        return Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly)
            .Select(p => new ThemeFileEntry(Path.GetFileName(p), CanonicalFilePath.Normalize(p)))
            .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

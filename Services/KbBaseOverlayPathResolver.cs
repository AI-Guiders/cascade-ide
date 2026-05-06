using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>Валидация пути оверлея KB-Base: должен содержать <c>knowledge/</c>.</summary>
internal static class KbBaseOverlayPathResolver
{
    internal static string? TryResolveCanonRoot(CascadeIdeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var raw = settings.AgentNotes.KbBaseOverlayPath.Trim();
        if (raw.Length == 0)
            return null;

        var expanded = Environment.ExpandEnvironmentVariables(raw);
        var path = Path.IsPathRooted(expanded)
            ? Path.GetFullPath(expanded)
            : Path.GetFullPath(Path.Combine(UserSettingsPaths.GetSettingsDirectory(), expanded));

        var knowledge = Path.Combine(path, "knowledge");
        return Directory.Exists(knowledge) ? path : null;
    }
}

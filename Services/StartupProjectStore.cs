using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Сохранение выбранного стартового проекта для отладки; канон — <c>LaunchProfilesStore</c> / <c>launch-profiles.toml</c> (ADR 0090),
/// <c>startup-project.json</c> — зеркалирование для совместимости.
/// </summary>
public static class StartupProjectStore
{
    private const string FileName = "startup-project.json";

    public static string GetStorePath(string solutionPath)
    {
        var root = BreakpointsFileService.GetWorkspaceRoot(solutionPath);
        if (string.IsNullOrEmpty(root))
            return "";
        return Path.Combine(root, ".cascade-ide", FileName);
    }

    public static bool TryLoad(string solutionPath, out string? projectPathRelative)
    {
        if (LaunchProfilesStore.TryGetActiveProjectRelativePath(solutionPath, out var fromToml, out _) &&
            !string.IsNullOrEmpty(fromToml))
        {
            projectPathRelative = fromToml;
            return true;
        }

        return TryLoadLegacyJsonOnly(solutionPath, out projectPathRelative);
    }

    /// <summary>Только <c>startup-project.json</c>, без чтения TOML (миграция / fallback).</summary>
    internal static bool TryLoadLegacyJsonOnly(string solutionPath, out string? projectPathRelative)
    {
        projectPathRelative = null;
        var storePath = GetStorePath(solutionPath);
        if (string.IsNullOrEmpty(storePath) || !File.Exists(storePath))
            return false;

        try
        {
            var json = File.ReadAllText(storePath);
            var dto = JsonSerializer.Deserialize<Dto>(json);
            var rel = dto?.StartupProjectRelativePath?.Trim().Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(rel))
                return false;
            projectPathRelative = rel;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Save(string solutionPath, string projectPathRelativeToSolution)
    {
        LaunchProfilesStore.UpsertActiveProject(solutionPath, projectPathRelativeToSolution);
        var storePath = GetStorePath(solutionPath);
        if (string.IsNullOrEmpty(storePath))
            return;
        var rel = projectPathRelativeToSolution.Trim().Replace('/', Path.DirectorySeparatorChar);
        var dir = Path.GetDirectoryName(storePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var dto = new Dto { StartupProjectRelativePath = rel };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(storePath, json);
    }

    public static void Clear(string solutionPath)
    {
        LaunchProfilesStore.Delete(solutionPath);
        var storePath = GetStorePath(solutionPath);
        if (!string.IsNullOrEmpty(storePath) && File.Exists(storePath))
        {
            try { File.Delete(storePath); }
            catch { /* ignore */ }
        }
    }

    private sealed class Dto
    {
        public string? StartupProjectRelativePath { get; set; }
    }
}

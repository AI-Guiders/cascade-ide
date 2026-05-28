#nullable enable
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Шипнутые пресеты подписей ветвей IF: <c>CodeNavigation/condition-branch-label-presets.toml</c>
/// (диск / embedded); overlay репозитория <c>.cascade/workspace.toml</c>;
/// затем <c>[[code_navigation_map.condition_branch.presets]]</c> в settings.
/// Merge по <see cref="CodeNavigationMapConditionBranchPresetEntry.Id"/>.
/// </summary>
public static class CodeNavigationMapConditionBranchPresetsLoader
{
    public const string BundledRelativePath = "CodeNavigation/condition-branch-label-presets.toml";

    private sealed class BundledRoot
    {
        public CodeNavigationMapSettings? CodeNavigationMap { get; set; }
    }

    public static string GetEmbeddedBundledToml()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Missing bundled {BundledRelativePath} (disk under AppContext.BaseDirectory or embedded resource in CascadeIDE assembly).");
        return text;
    }

    public static IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> LoadBundledEntriesOrFallback()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var raw))
            return [];
        try
        {
            var root = CascadeTomlSerializer.Deserialize<BundledRoot>(raw.Trim());
            return root?.CodeNavigationMap?.ConditionBranch?.Presets is { Count: > 0 } list
                ? list
                : [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Бандл → репо (<paramref name="solutionPath"/>) → пользовательский overlay в <paramref name="map"/>.</summary>
    public static IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> GetEffectivePresets(
        CodeNavigationMapSettings? map,
        string? solutionPath = null)
    {
        var bundled = LoadBundledEntriesOrFallback();
        var repository = LoadRepositoryPresetsFromSolutionDirectory(solutionPath);
        var afterRepo = MergeLayers(bundled, repository);
        var user = map?.ConditionBranch?.Presets ?? [];
        return MergeLayers(afterRepo, user);
    }

    /// <summary>Читает <c>.cascade/workspace.toml</c>; возвращает пресеты ветвей или пустой список.</summary>
    public static IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> LoadRepositoryPresetsFromSolutionDirectory(
        string? solutionPath)
    {
        var dir = NormalizeRepositoryRoot(solutionPath);
        if (dir is null)
            return [];

        try
        {
            var path = Path.Combine(dir, ".cascade", "workspace.toml");
            if (!File.Exists(path))
                return [];

            var text = File.ReadAllText(path);
            var ui = CascadeTomlSerializer.Deserialize<Features.Workspace.RepositoryWorkspaceToml>(text);
            if (ui?.CodeNavigationMap?.ConditionBranch?.Presets is not { Count: > 0 } presets)
                return [];

            return presets;
        }
        catch
        {
            return [];
        }
    }

    public static List<CodeNavigationMapConditionBranchPresetEntry> MergeLayers(
        IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> baseLayer,
        IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> overlay)
    {
        var dict = new Dictionary<string, CodeNavigationMapConditionBranchPresetEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in baseLayer)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                continue;
            dict[e.Id.Trim()] = CloneEntry(e);
        }

        foreach (var e in overlay)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                continue;
            dict[e.Id.Trim()] = CloneEntry(e);
        }

        return dict.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>Alias for tests: bundled + user without repository.</summary>
    public static List<CodeNavigationMapConditionBranchPresetEntry> MergeBundledWithUser(
        IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> bundled,
        IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> user) =>
        MergeLayers(bundled, user);

    private static string? NormalizeRepositoryRoot(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return null;
        try
        {
            var p = CanonicalFilePath.Normalize(solutionPath.Trim());
            if (File.Exists(p))
                return Path.GetDirectoryName(p);
            return Directory.Exists(p) ? p : null;
        }
        catch
        {
            return null;
        }
    }

    private static CodeNavigationMapConditionBranchPresetEntry CloneEntry(CodeNavigationMapConditionBranchPresetEntry e) =>
        new()
        {
            Id = e.Id,
            Positive = e.Positive,
            Negative = e.Negative
        };
}

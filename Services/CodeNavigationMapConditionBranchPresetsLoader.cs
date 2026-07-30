#nullable enable
using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Models;
using System.Reflection;

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

    /// <summary>Assembly with embedded presets (host or GlassCore). Default: this type's assembly.</summary>
    public static Assembly? EmbeddedPresetsAssembly { get; set; }

    private sealed class BundledRoot
    {
        public CodeNavigationMapSettings? CodeNavigationMap { get; set; }
    }

    /// <summary>Minimal workspace.toml slice — avoids pulling UiChrome workspace chrome DTOs into GlassCore.</summary>
    private sealed class WorkspaceConditionBranchRoot
    {
        public CodeNavigationMapSettings? CodeNavigationMap { get; set; }
    }

    public static string GetEmbeddedBundledToml()
    {
        if (!TryReadBundled(out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Missing bundled {BundledRelativePath} (disk under AppContext.BaseDirectory or embedded resource).");
        return text;
    }

    public static IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry> LoadBundledEntriesOrFallback()
    {
        if (!TryReadBundled(out var raw) || string.IsNullOrWhiteSpace(raw))
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
            var text = TextFileReadWrite.TryReadAllTextIfExists(path);
            if (text is null)
                return [];

            var ui = CascadeTomlSerializer.Deserialize<WorkspaceConditionBranchRoot>(text);
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

    static bool TryReadBundled([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? text)
    {
        text = null;
        var disk = Path.Combine(
            AppContext.BaseDirectory,
            BundledRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var diskText = TextFileReadWrite.TryReadAllTextIfExists(disk);
        if (diskText is not null)
        {
            text = diskText;
            return true;
        }

        var asm = EmbeddedPresetsAssembly ?? typeof(CodeNavigationMapConditionBranchPresetsLoader).Assembly;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.EndsWith("condition-branch-label-presets.toml", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("condition-branch-label-presets.toml", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null)
                continue;
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
            return !string.IsNullOrWhiteSpace(text);
        }

        return false;
    }

    private static string? NormalizeRepositoryRoot(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return null;
        try
        {
            var p = Path.GetFullPath(solutionPath.Trim());
            if (File.Exists(p))
                return Path.GetDirectoryName(p);
            if (Directory.Exists(p))
                return p;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static CodeNavigationMapConditionBranchPresetEntry CloneEntry(
        CodeNavigationMapConditionBranchPresetEntry e) =>
        new()
        {
            Id = e.Id,
            Positive = e.Positive,
            Negative = e.Negative,
        };
}

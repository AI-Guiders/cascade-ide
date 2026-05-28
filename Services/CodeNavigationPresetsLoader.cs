#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Шипнутые пресеты: по умолчанию — встроенный ресурс (и опционально файл рядом с exe); затем overlay репозитория
/// <c>.cascade/workspace.toml</c> (<c>[[code_navigation.presets]]</c>); затем пользовательский <c>settings.toml</c>.
/// Merge по <see cref="CodeNavigationPresetEntry.Id"/> на каждом шаге (последний слой побеждает).
/// Внутренняя цепочка <see cref="CodeNavigationPresetMerge"/> по-прежнему получает JSON-строку (контракт merge не менялся).
/// </summary>
public static class CodeNavigationPresetsLoader
{
    private static readonly JsonSerializerOptions s_mergeJson = new() { WriteIndented = false };

    /// <summary>Относительный путь от <see cref="AppContext.BaseDirectory"/> (опциональный override поверх встроенного бандла).</summary>
    public const string BundledRelativePath = "CodeNavigation/presets.toml";

    /// <summary>Корень шипнутого TOML (<c>[code_navigation]</c> / <c>[[code_navigation.presets]]</c>).</summary>
    private sealed class BundledPresetsRoot
    {
        public CodeNavigationSettings? CodeNavigation { get; set; }
    }

    private sealed class PresetMergeWire
    {
        [JsonPropertyName("include_kinds")]
        public List<string>? IncludeKinds { get; set; }

        [JsonPropertyName("exclude_kinds")]
        public List<string>? ExcludeKinds { get; set; }
    }

    /// <summary>
    /// Текст шипнутого <c>CodeNavigation/presets.toml</c>: как в рантайме — сначала файл под <see cref="AppContext.BaseDirectory"/>,
    /// иначе <see cref="BundledAppContent"/> (EmbeddedResource). Так тесты и агенты не зависят от устаревшей копии <c>CascadeIDE.dll</c> без ресурсов.
    /// </summary>
    /// <exception cref="InvalidOperationException">Нет ни файла рядом с процессом, ни встроенного ресурса.</exception>
    public static string GetEmbeddedBundledPresetsToml()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Missing bundled {BundledRelativePath} (disk under AppContext.BaseDirectory or embedded resource in CascadeIDE assembly).");
        return text;
    }

    /// <summary>
    /// Тот же JSON, что внутри уходит в <see cref="CodeNavigationPresetMerge.Merge"/> после разбора бандла без пользовательского overlay.
    /// </summary>
    public static string ToPresetMergeJsonFromBundledToml(string bundledToml)
    {
        var root = CascadeTomlSerializer.Deserialize<BundledPresetsRoot>(bundledToml.Trim());
        if (root?.CodeNavigation?.Presets is not { Count: > 0 })
            return "{}";

        var merged = MergeBundledWithUser(root.CodeNavigation.Presets, []);
        return PresetEntriesToMergeJson(merged);
    }

    /// <summary>JSON для <see cref="CodeNavigationPresetMerge"/>: бандл → репо → пользовательские настройки.</summary>
    /// <param name="settings">Из <c>%LocalAppData%\CascadeIDE\settings.toml</c>.</param>
    /// <param name="solutionPath">Путь к <c>.sln</c> или к каталогу репозитория; для <c>.cascade/workspace.toml</c>. Пусто — только бандл + пользователь.</param>
    public static string GetEffectivePresetsJson(CodeNavigationSettings settings, string? solutionPath = null)
    {
        var bundled = LoadBundledEntriesOrFallback();
        var repository = LoadRepositoryPresetsFromSolutionDirectory(solutionPath);
        var afterRepo = MergeBundledWithUser(bundled, repository);
        var merged = MergeBundledWithUser(afterRepo, settings.Presets);
        return PresetEntriesToMergeJson(merged);
    }

    /// <summary>Читает <c>.cascade/workspace.toml</c> в корне репозитория; возвращает пресеты навигации или пустой список.</summary>
    /// <param name="solutionPath">Путь к <c>.sln</c> или к каталогу репозитория.</param>
    public static IReadOnlyList<CodeNavigationPresetEntry> LoadRepositoryPresetsFromSolutionDirectory(string? solutionPath)
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
            if (ui?.CodeNavigation?.Presets is not { Count: > 0 })
                return [];

            return ui.CodeNavigation.Presets;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Каталог репозитория: для пути к файлу решения — родительская папка (как <c>GetWorkspacePath</c> в VM).</summary>
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

    public static IReadOnlyList<CodeNavigationPresetEntry> LoadBundledEntriesOrFallback()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var raw))
            return [];
        try
        {
            var root = CascadeTomlSerializer.Deserialize<BundledPresetsRoot>(raw.Trim());
            if (root?.CodeNavigation?.Presets is { Count: > 0 })
                return root.CodeNavigation.Presets;
        }
        catch
        {
            // ignored
        }

        return [];
    }

    /// <summary>Пользовательские пресеты перекрывают бандл по совпадающему <see cref="CodeNavigationPresetEntry.Id"/>.</summary>
    public static List<CodeNavigationPresetEntry> MergeBundledWithUser(
        IReadOnlyList<CodeNavigationPresetEntry> bundled,
        IReadOnlyList<CodeNavigationPresetEntry> user)
    {
        var dict = new Dictionary<string, CodeNavigationPresetEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in bundled)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                continue;
            dict[e.Id.Trim()] = CloneEntry(e);
        }

        foreach (var e in user)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                continue;
            dict[e.Id.Trim()] = CloneEntry(e);
        }

        return dict.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    private static CodeNavigationPresetEntry CloneEntry(CodeNavigationPresetEntry e) =>
        new()
        {
            Id = e.Id,
            IncludeKinds = e.IncludeKinds?.ToList(),
            ExcludeKinds = e.ExcludeKinds?.ToList()
        };

    private static string PresetEntriesToMergeJson(IReadOnlyList<CodeNavigationPresetEntry> merged)
    {
        var dict = new Dictionary<string, PresetMergeWire>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in merged)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                continue;
            dict[e.Id.Trim()] = new PresetMergeWire
            {
                IncludeKinds = e.IncludeKinds,
                ExcludeKinds = e.ExcludeKinds
            };
        }

        return JsonSerializer.Serialize(dict, s_mergeJson);
    }
}

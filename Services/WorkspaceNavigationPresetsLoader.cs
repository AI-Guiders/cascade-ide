#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using Tomlyn;

namespace CascadeIDE.Services;

/// <summary>
/// Шипнутые пресеты: по умолчанию — встроенный ресурс (и опционально файл рядом с exe); затем overlay репозитория
/// <c>.cascade/workspace.toml</c> (<c>[[workspace_navigation_context.presets]]</c>, как в settings); затем пользовательский
/// <c>settings.toml</c>. Merge по <see cref="WorkspaceNavigationPresetEntry.Id"/> на каждом шаге (последний слой побеждает).
/// Внутренняя цепочка <see cref="WorkspaceNavigationPresetMerge"/> по-прежнему получает JSON-строку (контракт merge не менялся).
/// </summary>
public static class WorkspaceNavigationPresetsLoader
{
    private static readonly JsonSerializerOptions s_mergeJson = new() { WriteIndented = false };

    /// <summary>Относительный путь от <see cref="AppContext.BaseDirectory"/> (опциональный override поверх встроенного бандла).</summary>
    public const string BundledRelativePath = "WorkspaceNavigation/presets.toml";

    /// <summary>Корень шипнутого TOML (<c>[[presets]]</c>).</summary>
    private sealed class BundledPresetsRoot
    {
        [JsonPropertyName("presets")]
        public List<WorkspaceNavigationPresetEntry> Presets { get; set; } = new();
    }

    private sealed class PresetMergeWire
    {
        [JsonPropertyName("include_kinds")]
        public List<string>? IncludeKinds { get; set; }

        [JsonPropertyName("exclude_kinds")]
        public List<string>? ExcludeKinds { get; set; }
    }

    /// <summary>Текст встроенного <c>presets.toml</c> (единый источник с файлом в репозитории).</summary>
    /// <exception cref="InvalidOperationException">Ресурс не встроен в сборку (ошибка сборки).</exception>
    public static string GetEmbeddedBundledPresetsToml()
    {
        if (!BundledAppContent.TryReadEmbeddedText("WorkspaceNavigation/presets.toml", out var text))
            throw new InvalidOperationException("Missing embedded resource WorkspaceNavigation/presets.toml.");
        return text;
    }

    /// <summary>
    /// Тот же JSON, что внутри уходит в <see cref="WorkspaceNavigationPresetMerge.Merge"/> после разбора бандла без пользовательского overlay.
    /// </summary>
    public static string ToPresetMergeJsonFromBundledToml(string bundledToml)
    {
        var root = TomlSerializer.Deserialize<BundledPresetsRoot>(bundledToml.Trim());
        if (root?.Presets is not { Count: > 0 })
            return "{}";

        var merged = MergeBundledWithUser(root.Presets, []);
        return PresetEntriesToMergeJson(merged);
    }

    /// <summary>JSON для <see cref="WorkspaceNavigationPresetMerge"/>: бандл → репо → пользовательские настройки.</summary>
    /// <param name="settings">Из <c>%LocalAppData%\CascadeIDE\settings.toml</c>.</param>
    /// <param name="solutionPath">Путь к <c>.sln</c> или к каталогу репозитория; для <c>.cascade/workspace.toml</c>. Пусто — только бандл + пользователь.</param>
    public static string GetEffectivePresetsJson(WorkspaceNavigationContextSettings settings, string? solutionPath = null)
    {
        var bundled = LoadBundledEntriesOrFallback();
        var repository = LoadRepositoryPresetsFromSolutionDirectory(solutionPath);
        var afterRepo = MergeBundledWithUser(bundled, repository);
        var merged = MergeBundledWithUser(afterRepo, settings.Presets);
        return PresetEntriesToMergeJson(merged);
    }

    /// <summary>Читает <c>.cascade/workspace.toml</c> в корне репозитория; возвращает пресеты навигации или пустой список.</summary>
    /// <param name="solutionPath">Путь к <c>.sln</c> или к каталогу репозитория.</param>
    public static IReadOnlyList<WorkspaceNavigationPresetEntry> LoadRepositoryPresetsFromSolutionDirectory(string? solutionPath)
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
            var ui = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(text);
            if (ui?.WorkspaceNavigationContext?.Presets is not { Count: > 0 })
                return [];

            return ui.WorkspaceNavigationContext.Presets;
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
            var p = Path.GetFullPath(solutionPath.Trim());
            if (File.Exists(p))
                return Path.GetDirectoryName(p);
            return Directory.Exists(p) ? p : null;
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<WorkspaceNavigationPresetEntry> LoadBundledEntriesOrFallback()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var raw))
            return [];
        try
        {
            var root = TomlSerializer.Deserialize<BundledPresetsRoot>(raw.Trim());
            if (root?.Presets is { Count: > 0 })
                return root.Presets;
        }
        catch
        {
            // ignored
        }

        return [];
    }

    /// <summary>Пользовательские пресеты перекрывают бандл по совпадающему <see cref="WorkspaceNavigationPresetEntry.Id"/>.</summary>
    public static List<WorkspaceNavigationPresetEntry> MergeBundledWithUser(
        IReadOnlyList<WorkspaceNavigationPresetEntry> bundled,
        IReadOnlyList<WorkspaceNavigationPresetEntry> user)
    {
        var dict = new Dictionary<string, WorkspaceNavigationPresetEntry>(StringComparer.OrdinalIgnoreCase);
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

    private static WorkspaceNavigationPresetEntry CloneEntry(WorkspaceNavigationPresetEntry e) =>
        new()
        {
            Id = e.Id,
            IncludeKinds = e.IncludeKinds?.ToList(),
            ExcludeKinds = e.ExcludeKinds?.ToList()
        };

    private static string PresetEntriesToMergeJson(IReadOnlyList<WorkspaceNavigationPresetEntry> merged)
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

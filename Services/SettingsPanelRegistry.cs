using System.Reflection;
using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Автоматическая навигация настроек: <see cref="SettingsPanelAttribute"/> на <c>*PanelView</c>.
/// <c>settings-categories.toml</c> — опциональный overlay (порядок, подписи, скрытие).
/// </summary>
public static class SettingsPanelRegistry
{
    public static IReadOnlyList<SettingsCategoryDefinition> LoadOrdered()
    {
        var discovered = DiscoverFromAssembly();
        if (discovered.Count == 0)
            discovered = SettingsCategoryCatalog.LoadBuiltinFallbackList();

        if (!TryLoadTomlOverlay(out var overlay) || overlay.Count == 0)
            return discovered;

        return Merge(discovered, overlay);
    }

    public static IReadOnlyList<SettingsCategoryDefinition> DiscoverFromAssembly()
    {
        var assembly = typeof(SettingsPanelRegistry).Assembly;
        var list = new List<(SettingsPanelAttribute Attr, SettingsCategoryDefinition Def)>();

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(Control).IsAssignableFrom(type) || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<SettingsPanelAttribute>(inherit: false);
            if (attr is null || attr.Hidden)
                continue;

            var panel = attr.PanelId.Trim();
            var title = attr.Title.Trim();
            if (string.IsNullOrEmpty(panel) || string.IsNullOrEmpty(title))
                continue;

            list.Add((attr, new SettingsCategoryDefinition
            {
                Id = panel,
                Panel = panel,
                Title = title,
                Group = attr.Group.Trim(),
                Order = attr.Order,
            }));
        }

        return list
            .OrderBy(x => x.Attr.Order)
            .ThenBy(x => x.Def.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Def)
            .ToList();
    }

    internal static IReadOnlyList<SettingsCategoryDefinition> Merge(
        IReadOnlyList<SettingsCategoryDefinition> discovered,
        IReadOnlyList<SettingsCategoryDefinition> overlay)
    {
        var byPanel = discovered.ToDictionary(c => c.Panel, StringComparer.OrdinalIgnoreCase);
        var merged = new List<SettingsCategoryDefinition>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in overlay.OrderBy(c => c.Order).ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase))
        {
            if (row.Hidden)
            {
                used.Add(row.Panel);
                continue;
            }

            if (byPanel.TryGetValue(row.Panel, out var baseDef))
            {
                merged.Add(ApplyOverlay(baseDef, row));
                used.Add(row.Panel);
            }
            else
            {
                merged.Add(row);
                used.Add(row.Panel);
            }
        }

        foreach (var def in discovered)
        {
            if (!used.Contains(def.Panel))
                merged.Add(def);
        }

        return merged.Count > 0 ? merged : discovered;
    }

    private static SettingsCategoryDefinition ApplyOverlay(
        SettingsCategoryDefinition baseline,
        SettingsCategoryDefinition overlay) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(overlay.Id) ? baseline.Id : overlay.Id.Trim(),
            Panel = overlay.Panel,
            Title = string.IsNullOrWhiteSpace(overlay.Title) ? baseline.Title : overlay.Title.Trim(),
            Group = string.IsNullOrWhiteSpace(overlay.Group) ? baseline.Group : overlay.Group.Trim(),
            Order = overlay.Order != 0 ? overlay.Order : baseline.Order,
            Hidden = overlay.Hidden,
        };

    private static bool TryLoadTomlOverlay(out IReadOnlyList<SettingsCategoryDefinition> categories)
    {
        categories = [];
        if (!BundledAppContent.TryReadDiskThenEmbedded(SettingsCategoryCatalog.BundledRelativePath, out var text)
            || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        categories = SettingsCategoryCatalog.ParseTomlCategories(text);
        return categories.Count > 0;
    }
}

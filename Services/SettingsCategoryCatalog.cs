using CascadeIDE.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.Services;

/// <summary>
/// Парсер <c>Settings/settings-categories.toml</c> (опциональный overlay поверх <see cref="SettingsPanelRegistry"/>).
/// </summary>
public static class SettingsCategoryCatalog
{
    public const string BundledRelativePath = "Settings/settings-categories.toml";

    /// <summary>Каталог для UI: атрибуты на панелях + опциональный TOML-overlay.</summary>
    public static IReadOnlyList<SettingsCategoryDefinition> LoadOrdered() =>
        SettingsPanelRegistry.LoadOrdered();

    internal static IReadOnlyList<SettingsCategoryDefinition> LoadBuiltinFallbackList() =>
    [
        new() { Id = "themes", Group = "Оформление", Title = "Тема IDE", Panel = SettingsPanelIds.Themes, Order = 0 },
        new() { Id = "ai_chat", Group = "Агент", Title = "AI и чат", Panel = SettingsPanelIds.AiChat, Order = 10 },
        new() { Id = "editor", Group = "Редактор", Title = "Редактор", Panel = SettingsPanelIds.Editor, Order = 20 },
        new() { Id = "csharp_lsp", Group = "Редактор", Title = "C# / LSP", Panel = SettingsPanelIds.CSharpLsp, Order = 30 },
        new() { Id = "markdown", Group = "Контент", Title = "Markdown · диаграммы", Panel = SettingsPanelIds.Markdown, Order = 40 },
        new() { Id = "markdown_lsp", Group = "Контент", Title = "Markdown LSP", Panel = SettingsPanelIds.MarkdownLsp, Order = 50 },
        new() { Id = "hci", Group = "Индекс", Title = "Hybrid Codebase Index", Panel = SettingsPanelIds.Hci, Order = 60 },
    ];

    internal static IReadOnlyList<SettingsCategoryDefinition> ParseTomlCategories(string text)
    {
        try
        {
            if (TomlSerializer.Deserialize<TomlTable>(text, CascadeTomlSerializer.Options) is not TomlTable root)
                return [];

            if (!root.TryGetValue("settings_category", out var node))
                return [];

            var list = new List<SettingsCategoryDefinition>();
            switch (node)
            {
                case TomlTableArray arr:
                    foreach (var row in arr)
                        TryAddRow(row, list);
                    break;
                case TomlTable single:
                    TryAddRow(single, list);
                    break;
            }

            return list;
        }
        catch
        {
            return [];
        }
    }

    private static void TryAddRow(TomlTable row, List<SettingsCategoryDefinition> list)
    {
        var panel = GetString(row, "panel");
        if (string.IsNullOrWhiteSpace(panel))
            return;

        var title = GetString(row, "title");
        var id = GetString(row, "id");
        if (string.IsNullOrWhiteSpace(id))
            id = panel.Trim();

        list.Add(new SettingsCategoryDefinition
        {
            Id = id,
            Group = GetString(row, "group"),
            Title = title,
            Panel = panel.Trim(),
            Order = GetInt(row, "order"),
            Hidden = GetBool(row, "hidden"),
        });
    }

    private static string GetString(TomlTable row, string key) =>
        row.TryGetValue(key, out var v) ? v?.ToString()?.Trim() ?? "" : "";

    private static int GetInt(TomlTable row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null)
            return 0;
        return v switch
        {
            int i => i,
            long l => (int)l,
            _ => int.TryParse(v.ToString(), out var n) ? n : 0,
        };
    }

    private static bool GetBool(TomlTable row, string key) =>
        row.TryGetValue(key, out var v)
        && v is bool b
        && b;
}

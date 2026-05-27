namespace CascadeIDE.Models;

/// <summary>Строка <c>[[settings_category]]</c> из <c>Settings/settings-categories.toml</c>.</summary>
public sealed class SettingsCategoryDefinition
{
    public string Id { get; init; } = "";

    /// <summary>Заголовок группы в боковом списке (повторяется у соседних пунктов).</summary>
    public string Group { get; init; } = "";

    public string Title { get; init; } = "";

    /// <summary>Встроенная страница (<see cref="SettingsPanelIds"/>).</summary>
    public string Panel { get; init; } = "";

    /// <summary>Порядок в боковом списке (меньше — выше). 0 = по умолчанию из атрибута.</summary>
    public int Order { get; init; }

    /// <summary>Скрыть пункт (TOML: <c>hidden = true</c>).</summary>
    public bool Hidden { get; init; }
}

/// <summary>Идентификаторы встроенных страниц настроек.</summary>
public static class SettingsPanelIds
{
    public const string Themes = "themes";
    public const string AiChat = "ai_chat";
    public const string Editor = "editor";
    public const string CSharpLsp = "csharp_lsp";
    public const string Markdown = "markdown";
    public const string MarkdownLsp = "markdown_lsp";
    public const string Hci = "hci";
    public const string WorkspaceNavigationMap = "workspace_navigation_map";
}

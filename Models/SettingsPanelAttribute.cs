namespace CascadeIDE.Models;

/// <summary>
/// Регистрация страницы настроек для автоматического бокового списка (см. <see cref="Services.SettingsPanelRegistry"/>).
/// Вешать на code-behind <c>*SettingsPanelView</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SettingsPanelAttribute : Attribute
{
    public SettingsPanelAttribute(string panelId, string title, string group = "", int order = 100)
    {
        PanelId = panelId;
        Title = title;
        Group = group;
        Order = order;
    }

    /// <summary>Идентификатор панели (<see cref="SettingsPanelIds"/>).</summary>
    public string PanelId { get; }

    public string Title { get; }

    public string Group { get; }

    public int Order { get; }

    /// <summary>Скрыть пункт в списке (TOML override или тесты).</summary>
    public bool Hidden { get; set; }
}

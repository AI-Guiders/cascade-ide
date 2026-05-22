namespace CascadeIDE.Features.Settings;

/// <summary>Элемент бокового списка настроек (заголовок группы или выбираемая категория).</summary>
public abstract record SettingsNavigationItem;

public sealed record SettingsNavigationGroupHeader(string Title) : SettingsNavigationItem;

public sealed record SettingsNavigationCategory(
    string Id,
    string Group,
    string Title,
    string Panel,
    bool ShowGroupHeader) : SettingsNavigationItem;

namespace CascadeIDE.Models;

/// <summary>Пресеты фильтра видов навигации (ADR 0039). TOML: <c>[workspace_navigation]</c>, <c>[[workspace_navigation.presets]]</c>.</summary>
public sealed class NavigationSettings
{
    public List<WorkspaceNavigationPresetEntry> Presets { get; set; } = [];
}

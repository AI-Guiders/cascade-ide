namespace CascadeIDE.Models;

/// <summary>Пресеты фильтра видов навигации по коду (ADR 0039, CNC). TOML: <c>[code_navigation]</c>, <c>[[code_navigation.presets]]</c>.</summary>
public sealed class CodeNavigationSettings
{
    public List<CodeNavigationPresetEntry> Presets { get; set; } = [];
}

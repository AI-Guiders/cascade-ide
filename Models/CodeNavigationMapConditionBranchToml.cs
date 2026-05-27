namespace CascadeIDE.Models;

/// <summary>TOML: <c>[code_navigation_map.condition_branch]</c>, <c>[[code_navigation_map.condition_branch.presets]]</c>.</summary>
public sealed class CodeNavigationMapConditionBranchToml
{
    public List<CodeNavigationMapConditionBranchPresetEntry> Presets { get; set; } = [];
}

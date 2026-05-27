using System.Text.Json.Serialization;

namespace CascadeIDE.Models;

/// <summary>Один пресет подписей ветвей IF (TOML: <c>[[code_navigation_map.condition_branch.presets]]</c>).</summary>
public sealed class CodeNavigationMapConditionBranchPresetEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("positive")]
    public string? Positive { get; set; }

    [JsonPropertyName("negative")]
    public string? Negative { get; set; }
}

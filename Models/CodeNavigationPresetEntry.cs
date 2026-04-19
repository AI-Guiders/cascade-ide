using System.Text.Json.Serialization;

namespace CascadeIDE.Models;

/// <summary>Один пресет фильтра навигации по коду (TOML: <c>[[code_navigation.presets]]</c>).</summary>
public sealed class CodeNavigationPresetEntry
{
    /// <summary>Идентификатор пресета (ключ в merge).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("include_kinds")]
    public List<string>? IncludeKinds { get; set; }

    [JsonPropertyName("exclude_kinds")]
    public List<string>? ExcludeKinds { get; set; }
}

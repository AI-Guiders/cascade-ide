using System.Text.Json.Serialization;

namespace CascadeIDE.Models;

/// <summary>Один пресет фильтра навигации (TOML: <c>[[workspace_navigation_context.presets]]</c>).</summary>
public sealed class WorkspaceNavigationPresetEntry
{
    /// <summary>Идентификатор пресета (ключ в merge).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("include_kinds")]
    public List<string>? IncludeKinds { get; set; }

    [JsonPropertyName("exclude_kinds")]
    public List<string>? ExcludeKinds { get; set; }
}

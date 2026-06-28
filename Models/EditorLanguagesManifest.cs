using System.Text.Json.Serialization;

namespace CascadeIDE.Models;

/// <summary>Корень <c>Settings/editor-languages.toml</c> — манифест языков Forward-редактора (Monaco).</summary>
public sealed class EditorLanguagesManifest
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    public List<EditorLanguageEntry> Languages { get; set; } = [];

    [JsonPropertyName("plain_text")]
    public List<EditorPlainTextEntry> PlainText { get; set; } = [];
}

/// <summary>Один язык с подсветкой: <c>[[languages]]</c>.</summary>
public sealed class EditorLanguageEntry
{
    public string Id { get; set; } = "";

    public string Display { get; set; } = "";

    public List<string> Extensions { get; set; } = [];

    public string? Monaco { get; set; }

    /// <summary>Учитывать в <see cref="Services.EditorLanguageSupport.IsTextFilePath"/> (default true).</summary>
    public bool Attach { get; set; } = true;
}

/// <summary>Текст без грамматики: <c>[[plain_text]]</c>.</summary>
public sealed class EditorPlainTextEntry
{
    public List<string> Extensions { get; set; } = [];

    public bool Attach { get; set; } = true;
}

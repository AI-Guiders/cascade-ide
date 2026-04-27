namespace CascadeIDE.Models;

/// <summary>Настройки редактора. TOML: <c>[editor]</c>.</summary>
public sealed class EditorSettings
{
    /// <summary>Inlay hints: включение и детализация. TOML: <c>[editor.inline_hints]</c>.</summary>
    public EditorInlineHintsSettings InlineHints { get; set; } = new();
}

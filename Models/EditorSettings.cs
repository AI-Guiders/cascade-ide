namespace CascadeIDE.Models;

/// <summary>Настройки редактора. TOML: <c>[editor]</c>.</summary>
public sealed class EditorSettings
{
    /// <summary>Inlay hints: включение и детализация. TOML: <c>[editor.inline_hints]</c>.</summary>
    public EditorInlineHintsSettings InlineHints { get; set; } = new();

    /// <summary>Debug hints (EOL значения в режиме остановки). TOML: <c>[editor.debug_hints]</c>.</summary>
    public EditorDebugHintsSettings DebugHints { get; set; } = new();
}

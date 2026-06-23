namespace CascadeIDE.Models;

/// <summary>Настройки редактора. TOML: <c>[editor]</c>.</summary>
public sealed class EditorSettings
{
    /// <summary>
    /// Forward host: <c>monaco_webview2</c> (ADR 0163). <c>avalonia_edit</c> — deprecated alias.
    /// </summary>
    public string ForwardHost { get; set; } = EditorForwardHostKindParser.MonacoWebView2Toml;

    public EditorForwardHostKind ResolveForwardHost() => EditorForwardHostKindParser.Parse(ForwardHost);

    /// <summary>Inlay hints: включение и детализация. TOML: <c>[editor.inline_hints]</c>.</summary>
    public EditorInlineHintsSettings InlineHints { get; set; } = new();

    /// <summary>Debug hints (EOL значения в режиме остановки). TOML: <c>[editor.debug_hints]</c>.</summary>
    public EditorDebugHintsSettings DebugHints { get; set; } = new();
}

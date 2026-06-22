namespace CascadeIDE.Models;

/// <summary>Хост Forward-редактора (ADR 0162). TOML: <c>[editor].forward_host</c>.</summary>
public enum EditorForwardHostKind
{
    AvaloniaEdit = 0,
    MonacoWebView2 = 1,
}

public static class EditorForwardHostKindParser
{
    public const string AvaloniaEditToml = "avalonia_edit";
    public const string MonacoWebView2Toml = "monaco_webview2";

    public static EditorForwardHostKind Parse(string? raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            MonacoWebView2Toml => EditorForwardHostKind.MonacoWebView2,
            AvaloniaEditToml or "" or null => EditorForwardHostKind.AvaloniaEdit,
            _ => EditorForwardHostKind.AvaloniaEdit,
        };

    public static string ToToml(EditorForwardHostKind kind) =>
        kind switch
        {
            EditorForwardHostKind.MonacoWebView2 => MonacoWebView2Toml,
            _ => AvaloniaEditToml,
        };
}

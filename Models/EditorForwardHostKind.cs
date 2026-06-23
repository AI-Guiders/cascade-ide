namespace CascadeIDE.Models;

/// <summary>Хост Forward-редактора (ADR 0163). TOML: <c>[editor].forward_host</c>.</summary>
public enum EditorForwardHostKind
{
    /// <summary>Deprecated — maps to <see cref="MonacoWebView2"/>.</summary>
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
            MonacoWebView2Toml or "" or null => EditorForwardHostKind.MonacoWebView2,
            AvaloniaEditToml => EditorForwardHostKind.MonacoWebView2,
            _ => EditorForwardHostKind.MonacoWebView2,
        };

    public static string ToToml(EditorForwardHostKind kind) => MonacoWebView2Toml;

    public static bool IsDeprecatedValue(string? raw) =>
        string.Equals(raw?.Trim(), AvaloniaEditToml, StringComparison.OrdinalIgnoreCase);
}

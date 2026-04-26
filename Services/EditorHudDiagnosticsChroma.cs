using Avalonia.Media;

namespace CascadeIDE.Services;

/// <summary>
/// Семантические цвета Editor HUD: squiggles, MFD/Problems — один sRGB-ряд (editor-forward-ui-cleanup).
/// Ключи в <c>App.axaml</c> совпадают с <see cref="EditorHudSeverityKeys"/>; тулбар-красный — отдельно (<c>CascadeTheme.ToolbarErrorForeground</c>).
/// </summary>
public static class EditorHudDiagnosticsChroma
{
    public static Color Error { get; } = Color.FromRgb(178, 92, 98);

    public static Color Warning { get; } = Color.FromRgb(168, 138, 72);

    public static Color Info { get; } = Color.FromRgb(96, 130, 150);

    public static Color InlayLabel { get; } = Color.FromRgb(122, 132, 148);

    public static IBrush InlayLabelBrush { get; } = new SolidColorBrush(InlayLabel);
}

/// <summary>Ключи <c>App.axaml</c> (DynamicResource в MFD).</summary>
public static class EditorHudSeverityKeys
{
    public const string Error = "EditorHud.SeverityError";
    public const string Warning = "EditorHud.SeverityWarning";
    public const string Info = "EditorHud.SeverityInfo";
}

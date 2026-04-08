namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Текущее размещение превью Markdown из merged <c>workspace.toml</c> (бандл + <c>.cascade/workspace.toml</c>).
/// </summary>
public static class MarkdownPreviewPlacementRuntime
{
    /// <summary>Значение по умолчанию до загрузки TOML и при сбросе тестов.</summary>
    public const MarkdownPreviewPlacement DefaultPlacement = MarkdownPreviewPlacement.ForwardSplit;

    public static MarkdownPreviewPlacement Current { get; private set; } = DefaultPlacement;

    internal static void ResetToCodeDefaults() => Current = DefaultPlacement;

    internal static void ApplyWorkspaceToml(UiWorkspaceToml? w)
    {
        ResetToCodeDefaults();
        if (w is null)
            return;
        if (!string.IsNullOrWhiteSpace(w.MarkdownPreviewPlacement))
            Current = MarkdownPreviewPlacementParser.ParseOrDefault(w.MarkdownPreviewPlacement, DefaultPlacement);
    }
}

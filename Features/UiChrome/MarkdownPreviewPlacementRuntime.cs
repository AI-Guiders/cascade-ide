namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Текущее размещение preview Markdown из merged <c>workspace.toml</c> (бандл + <c>.cascade/workspace.toml</c>).
/// </summary>
public static class MarkdownPreviewPlacementRuntime
{
    /// <summary>Значение по умолчанию до загрузки TOML и при сбросе тестов.</summary>
    public const MarkdownPreviewPlacement DefaultPlacement = MarkdownPreviewPlacement.Mfd;

    public static MarkdownPreviewPlacement Current { get; private set; } = DefaultPlacement;

    internal static void ResetToCodeDefaults() => Current = DefaultPlacement;

    internal static void ApplyWorkspaceToml(Features.Workspace.RepositoryWorkspaceToml? w)
    {
        ResetToCodeDefaults();
        if (w is null)
            return;
        var placement = w.Chrome?.MarkdownPreviewPlacement;
        if (!string.IsNullOrWhiteSpace(placement))
            Current = MarkdownPreviewPlacementParser.ParseOrDefault(placement, DefaultPlacement);
    }
}

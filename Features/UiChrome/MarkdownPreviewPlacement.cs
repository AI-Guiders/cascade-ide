namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Куда показывать preview Markdown (TOML: <c>markdown_preview_placement</c> в <c>UiModes/workspace.toml</c>).
/// </summary>
public enum MarkdownPreviewPlacement
{
    /// <summary>Зона MFD / вторичный контур — primary placement.</summary>
    Mfd,

    /// <summary>Отдельное окно <see cref="Views.MarkdownPreviewWindow"/>.</summary>
    SeparateWindow,
}

/// <summary>Парсинг значений из TOML/строк.</summary>
public static class MarkdownPreviewPlacementParser
{
    /// <summary>
    /// Допустимые строки: <c>mfd</c>, <c>separate_window</c>, <c>window</c> (синоним окна).
    /// Неизвестное или пустое — <paramref name="defaultValue"/>.
    /// </summary>
    public static MarkdownPreviewPlacement ParseOrDefault(string? s, MarkdownPreviewPlacement defaultValue)
    {
        if (string.IsNullOrWhiteSpace(s))
            return defaultValue;
        return s.Trim().ToLowerInvariant() switch
        {
            "mfd" => MarkdownPreviewPlacement.Mfd,
            "separate_window" or "window" => MarkdownPreviewPlacement.SeparateWindow,
            _ => defaultValue,
        };
    }
}

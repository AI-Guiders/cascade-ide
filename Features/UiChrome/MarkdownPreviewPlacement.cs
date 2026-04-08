namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Куда показывать превью Markdown (TOML: <c>markdown_preview_placement</c> в <c>UiModes/workspace.toml</c>).
/// </summary>
public enum MarkdownPreviewPlacement
{
    /// <summary>Колонка рядом с редактором во Forward (<c>EditorContentGrid</c>), если есть в вёрстке.</summary>
    ForwardSplit,

    /// <summary>Зона MFD (вкладка/панель) — пока не подключено к UI, используется отдельное окно.</summary>
    Mfd,

    /// <summary>Отдельное окно <see cref="Views.MarkdownPreviewWindow"/>.</summary>
    SeparateWindow,
}

/// <summary>Парсинг значений из TOML/строк.</summary>
public static class MarkdownPreviewPlacementParser
{
    /// <summary>
    /// Допустимые строки: <c>forward_split</c>, <c>mfd</c>, <c>separate_window</c>, <c>window</c> (синоним окна).
    /// Неизвестное или пустое — <paramref name="defaultValue"/>.
    /// </summary>
    public static MarkdownPreviewPlacement ParseOrDefault(string? s, MarkdownPreviewPlacement defaultValue)
    {
        if (string.IsNullOrWhiteSpace(s))
            return defaultValue;
        return s.Trim().ToLowerInvariant() switch
        {
            "forward_split" => MarkdownPreviewPlacement.ForwardSplit,
            "mfd" => MarkdownPreviewPlacement.Mfd,
            "separate_window" or "window" => MarkdownPreviewPlacement.SeparateWindow,
            _ => defaultValue,
        };
    }
}

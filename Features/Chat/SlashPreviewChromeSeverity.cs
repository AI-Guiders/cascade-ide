#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Визуальная severity slash-preview (1:1 с <c>SkiaStatusChipSeverity</c>).
/// Единый источник маппинга — <see cref="SlashCommandPreviewVisualMapper"/>.
/// </summary>
public enum SlashPreviewChromeSeverity
{
    None = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Info = 5,
}

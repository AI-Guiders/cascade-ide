#nullable enable

using CascadeIDE.Features.Chat;
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Палитра slash-preview: Kind → <see cref="SlashCommandPreviewVisualMapper"/> → chip.</summary>
internal static class SkiaSlashPreviewChrome
{
    public static SKColor PreviewTextColor(ISkiaKitPaintTheme theme, SlashCommandPreviewKind kind) =>
        ChipColors(theme, kind).Accent;

    public static SkiaStatusChipColors ChipColors(
        ISkiaKitPaintTheme theme,
        SlashCommandPreviewKind kind) =>
        SkiaStatusChip.ResolveColors(theme, ToChipSeverity(kind));

    internal static SkiaStatusChipSeverity ToChipSeverity(SlashCommandPreviewKind kind) =>
        ToChipSeverity(SlashCommandPreviewVisualMapper.ToChromeSeverity(kind));

    internal static SkiaStatusChipSeverity ToChipSeverity(SlashPreviewChromeSeverity severity) =>
        severity switch
        {
            SlashPreviewChromeSeverity.Success => SkiaStatusChipSeverity.Success,
            SlashPreviewChromeSeverity.Warning => SkiaStatusChipSeverity.Warning,
            SlashPreviewChromeSeverity.Error => SkiaStatusChipSeverity.Error,
            SlashPreviewChromeSeverity.Info => SkiaStatusChipSeverity.Info,
            SlashPreviewChromeSeverity.None => SkiaStatusChipSeverity.None,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unmapped SlashPreviewChromeSeverity."),
        };
}

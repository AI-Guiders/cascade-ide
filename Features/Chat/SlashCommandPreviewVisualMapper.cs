#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Единственная точка маппинга <see cref="SlashCommandPreviewKind"/> → chrome severity (P0).</summary>
public static class SlashCommandPreviewVisualMapper
{
    public static SlashPreviewChromeSeverity ToChromeSeverity(SlashCommandPreviewKind kind) =>
        kind switch
        {
            SlashCommandPreviewKind.Ok => SlashPreviewChromeSeverity.Success,
            SlashCommandPreviewKind.Incomplete => SlashPreviewChromeSeverity.Warning,
            SlashCommandPreviewKind.Error => SlashPreviewChromeSeverity.Error,
            SlashCommandPreviewKind.Hint => SlashPreviewChromeSeverity.Info,
            SlashCommandPreviewKind.None => SlashPreviewChromeSeverity.None,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unmapped SlashCommandPreviewKind."),
        };

    public static bool ShouldDrawChip(SlashCommandPreviewKind kind, string? text) =>
        ToChromeSeverity(kind) != SlashPreviewChromeSeverity.None
        && !string.IsNullOrWhiteSpace(text)
        && text.TrimStart().StartsWith('/');

    /// <summary>Все значения enum, кроме <see cref="SlashCommandPreviewKind.None"/>.</summary>
    public static IReadOnlyList<SlashCommandPreviewKind> NonNoneKinds { get; } =
        Enum.GetValues<SlashCommandPreviewKind>()
            .Where(static k => k != SlashCommandPreviewKind.None)
            .ToArray();
}

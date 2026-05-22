#nullable enable

namespace CascadeIDE.Features.Chat;

public readonly record struct SlashCommandPreviewResult(
    string? Text,
    SlashCommandPreviewKind Kind)
{
    public static SlashCommandPreviewResult Empty { get; } = new(null, SlashCommandPreviewKind.None);

    public bool HasText => !string.IsNullOrWhiteSpace(Text);
}

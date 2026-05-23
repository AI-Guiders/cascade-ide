#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>A11y для slash TCI: pill не дублирует длинный текст — tooltip при необходимости (P1).</summary>
public static class SlashCommandPreviewAccessibility
{
    public static bool ShouldShowToolTip(SlashCommandPreviewKind kind) =>
        kind is SlashCommandPreviewKind.Error
            or SlashCommandPreviewKind.Incomplete
            or SlashCommandPreviewKind.Hint;

    public static string? FormatToolTip(in SlashCommandPreviewResult preview)
    {
        if (!ShouldShowToolTip(preview.Kind) || !preview.HasText)
            return null;

        return preview.Text!.Trim();
    }
}

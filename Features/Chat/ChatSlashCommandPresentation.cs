#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Строки для пузыря слэш-команды в ленте.</summary>
public static class ChatSlashCommandPresentation
{
    /// <summary>Позиция иконки статуса по умолчанию (позже — <c>settings.toml</c>).</summary>
    public static ChatSlashCommandStatusIconPlacement DefaultStatusIconPlacement { get; set; } =
        ChatSlashCommandStatusIconPlacement.TopRight;

    public static string FormatDisplayPath(string? rawLine)
    {
        var trimmed = (rawLine ?? "").Trim();
        if (trimmed.Length == 0 || trimmed[0] != '/')
            return trimmed;

        if (SlashLineResolver.TryResolveSlashLine(trimmed, out var line) && line.IsCatalogMatch)
            return line.CanonicalPath;

        var space = trimmed.IndexOf(' ');
        return space < 0 ? trimmed : trimmed[..space];
    }

    public static string? NormalizeArgsTail(string? argsTail) =>
        string.IsNullOrWhiteSpace(argsTail) ? null : argsTail.Trim();
}

namespace CascadeIDE.Features.Chat;

/// <summary>Строки для пузыря слэш-команды в ленте.</summary>
public static class ChatSlashCommandPresentation
{
    /// <summary>Позиция иконки статуса по умолчанию (позже — <c>settings.toml</c>).</summary>
    public static ChatSlashCommandStatusIconPlacement DefaultStatusIconPlacement { get; set; } =
        ChatSlashCommandStatusIconPlacement.TopRight;

    public static string FormatDisplayPath(in ChatSlashCommandParseResult parse, string rawLine)
    {
        if (parse.IsRejected)
        {
            var t = rawLine.Trim();
            var sp = t.IndexOf(' ');
            return sp < 0 ? t : t[..sp];
        }

        if (parse.Shape == ChatSlashCommandShape.NamespaceAction && !string.IsNullOrEmpty(parse.Action))
        {
            return string.IsNullOrEmpty(parse.SubAction)
                ? $"/{parse.Head} {parse.Action}"
                : $"/{parse.Head} {parse.Action} {parse.SubAction}";
        }

        return "/" + parse.Head;
    }

    public static string? NormalizeArgsTail(string? argsTail) =>
        string.IsNullOrWhiteSpace(argsTail) ? null : argsTail.Trim();
}

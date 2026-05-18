#nullable enable
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Chat;

/// <summary>Нормализация путей в хвосте slash workspace/file (ADR 0125).</summary>
public static class ChatSlashWorkspacePathHelper
{
    public static string? NormalizeArgsTail(string? tail) =>
        ChatSlashCommandPresentation.NormalizeArgsTail(tail);

    public static bool TryNormalizePathArgument(
        string? tail,
        string? workspaceRoot,
        out string? fullPath,
        out string? error)
    {
        fullPath = null;
        error = null;
        var trimmed = NormalizeArgsTail(tail);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            error = "Укажи путь в хвосте команды.";
            return false;
        }

        var unquoted = Unquote(trimmed);
        try
        {
            if (Path.IsPathRooted(unquoted))
                fullPath = CanonicalFilePath.Normalize(unquoted);
            else if (string.IsNullOrWhiteSpace(workspaceRoot))
                fullPath = CanonicalFilePath.Normalize(unquoted);
            else
                fullPath = CanonicalFilePath.Normalize(Path.Combine(workspaceRoot.Trim(), unquoted));
        }
        catch (Exception ex)
        {
            error = "Некорректный путь: " + ex.Message;
            return false;
        }

        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}

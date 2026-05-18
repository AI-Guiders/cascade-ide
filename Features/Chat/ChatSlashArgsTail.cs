#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Нормализация произвольного хвоста слэш-команды (пробелы, опциональные кавычки).</summary>
internal static class ChatSlashArgsTail
{
    /// <summary>
    /// Trim; если весь хвост в парных <c>"</c> или <c>'</c> — снять обёртку (удобно для <c>/git commit "feat: x"</c>).
    /// </summary>
    public static string NormalizeFreeText(string? argsTail)
    {
        var t = (argsTail ?? "").Trim();
        if (t.Length >= 2)
        {
            if (t[0] == '"' && t[^1] == '"')
                return t[1..^1];
            if (t[0] == '\'' && t[^1] == '\'')
                return t[1..^1];
        }

        return t;
    }
}

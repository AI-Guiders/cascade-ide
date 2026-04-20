namespace CascadeIDE.Services;

/// <summary>
/// Краткий заголовок команды для палитры, если записи в <see cref="IdeCommandPaletteCatalog"/> нет
/// (например alias в <c>intent-melody-aliases.toml</c> указывает на MCP-команду без отдельной строки палитры).
/// </summary>
public static class IdeCommandDocDisplay
{
    /// <summary>Текст до « returns:» из <see cref="IdeCommandsDoc"/>; иначе <paramref name="commandId"/>.</summary>
    public static string ShortTitleForCommandId(string commandId)
    {
        if (string.IsNullOrEmpty(commandId))
            return "";
        if (!IdeCommandsDoc.TryGetSummary(commandId, out var summary) || string.IsNullOrWhiteSpace(summary))
            return commandId;

        var s = summary.Trim();
        var i = s.IndexOf(" returns:", StringComparison.OrdinalIgnoreCase);
        if (i > 0)
            s = s[..i].TrimEnd();
        if (s.Length > 200)
            s = s[..197] + "…";
        return string.IsNullOrEmpty(s) ? commandId : s;
    }
}

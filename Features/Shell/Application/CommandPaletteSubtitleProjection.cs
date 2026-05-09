namespace CascadeIDE.Features.Shell.Application;

/// <summary>Подписи строк палитры команд (вторая строка / режим c:).</summary>
public static class CommandPaletteSubtitleProjection
{
    public static string CommandPaletteSubtitle(string commandId, string category)
    {
        var tail = string.IsNullOrEmpty(category) ? "" : category.Trim();
        return string.IsNullOrEmpty(tail) ? commandId : $"{commandId} · {tail}";
    }

    /// <summary>Вторая строка в режиме <c>c:</c> (мелодия уже в заголовке строки).</summary>
    public static string MelodyPaletteSecondaryLine(string entryTitle, string commandId, string category)
    {
        var tail = string.IsNullOrEmpty(category) ? "" : category.Trim();
        if (string.IsNullOrEmpty(tail))
            return $"{entryTitle} · {commandId}";
        return $"{entryTitle} · {commandId} · {tail}";
    }
}

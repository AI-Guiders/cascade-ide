using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Тексты подсказки/плейсхолдера оверлея палитры (без привязки к VM).</summary>
[PresentationProjection]
public static class CommandPaletteChromeProjection
{
    private static string ModeHints(string? melodyAliasesSample) =>
        string.IsNullOrEmpty(melodyAliasesSample)
            ? CommandPaletteChromeModeHints.SeparatorLineJoin
            : $"{CommandPaletteChromeModeHints.SeparatorLineJoin} ({melodyAliasesSample})";

    public static string FooterHint(string? toggleCommandPaletteHotkeyDisplay, string? melodyAliasesSampleForFooter)
    {
        var nav = ModeHints(melodyAliasesSampleForFooter);
        return !string.IsNullOrEmpty(toggleCommandPaletteHotkeyDisplay)
            ? $"↑↓ выбор · Enter выполнить · Esc закрыть · PgUp/PgDn страница · {toggleCommandPaletteHotkeyDisplay} выделить запрос · {nav}"
            : $"↑↓ выбор · Enter выполнить · Esc закрыть · PgUp/PgDn страница · {nav}";
    }

    public static string QueryPlaceholder(string? melodyAliasesSample)
    {
        var baseText = "Команда… · " + CommandPaletteChromeModeHints.SeparatorLineJoin;
        return string.IsNullOrEmpty(melodyAliasesSample)
            ? baseText
            : $"{baseText} ({melodyAliasesSample})";
    }
}

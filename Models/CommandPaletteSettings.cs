namespace CascadeIDE.Models;

/// <summary>Настройки палитры команд (Ctrl+Q). TOML: <c>[command_palette]</c> / вложенные таблицы.</summary>
public sealed class CommandPaletteSettings
{
    /// <summary>Подтаблица <c>[command_palette.go_to_search]</c>.</summary>
    public CommandPaletteGoToSearchSettings GoToSearch { get; set; } = new();
}

/// <summary>Workspace-поиск для префиксов <c>t:</c>/<c>m:</c>/<c>x:</c>; см. ADR 0112.</summary>
public sealed class CommandPaletteGoToSearchSettings
{
    /// <summary><c>rg</c>, <c>hci</c> или <c>auto</c> (строго в нижнем регистре в TOML).</summary>
    public string Backend { get; set; } = CommandPaletteGoToSearchBackendNormalizer.DefaultRaw;
}

/// <summary>Нормализация строки <see cref="CommandPaletteGoToSearchSettings.Backend"/>.</summary>
public static class CommandPaletteGoToSearchBackendNormalizer
{
    public const string RgRaw = "rg";
    public const string HciRaw = "hci";
    public const string AutoRaw = "auto";

    public static string DefaultRaw => RgRaw;

    /// <returns>Эффективный вид бэкенда; не распознанные значения → <see cref="CommandPaletteGoToSearchBackendKind.Rg"/>.</returns>
    public static CommandPaletteGoToSearchBackendKind Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return CommandPaletteGoToSearchBackendKind.Rg;
        return raw.Trim().ToLowerInvariant() switch
        {
            HciRaw => CommandPaletteGoToSearchBackendKind.Hci,
            AutoRaw => CommandPaletteGoToSearchBackendKind.Auto,
            RgRaw => CommandPaletteGoToSearchBackendKind.Rg,
            _ => CommandPaletteGoToSearchBackendKind.Rg,
        };
    }
}

public enum CommandPaletteGoToSearchBackendKind
{
    Rg = 0,
    Hci = 1,
    Auto = 2,
}

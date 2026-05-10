using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

public enum IdeCommandPaletteRowKind
{
    Command,
    GoTo,
    Hint,
}

/// <summary>Строка списка палитры команд (команда, go-to, подсказка).</summary>
public sealed class IdeCommandPaletteRowViewModel : ViewModelBase
{
    public IdeCommandPaletteRowViewModel(
        IdeCommandPaletteCatalog.Entry entry,
        string? hotkeyHint,
        UiModeFamily currentFamily,
        string? melodyAliasTail = null,
        string? argsJsonOverride = null)
    {
        RowKind = IdeCommandPaletteRowKind.Command;
        PaletteId = entry.PaletteId;
        CommandId = entry.CommandId;
        Category = entry.Category;
        if (!string.IsNullOrEmpty(melodyAliasTail))
        {
            // Режим c: — акцент на мелодии (первая строка), не на command_id.
            Title = $"c:{melodyAliasTail}";
            Subtitle = CommandPaletteSubtitleProjection.MelodyPaletteSecondaryLine(entry.Title, entry.CommandId, entry.Category);
        }
        else
        {
            Title = entry.Title;
            Subtitle = CommandPaletteSubtitleProjection.CommandPaletteSubtitle(entry.CommandId, entry.Category);
        }
        IsMelodyAccentRow = !string.IsNullOrEmpty(melodyAliasTail);
        ArgsJson = argsJsonOverride ?? entry.ArgsJson;
        HotkeyHint = hotkeyHint;
        IsAvailable = IdeCommandPaletteMatch.IsEntryAvailable(entry, currentFamily);
        UnavailableHint = IdeCommandPaletteMatch.UnavailableHint(entry, currentFamily);
    }

    /// <summary>Строка навигации (файл / совпадение rg).</summary>
    public IdeCommandPaletteRowViewModel(
        string title,
        string category,
        string fullPath,
        int line,
        int column,
        string prefixHint)
    {
        RowKind = IdeCommandPaletteRowKind.GoTo;
        PaletteId = "__goto__";
        CommandId = "__goto__";
        Title = title;
        Category = category;
        Subtitle = category;
        ArgsJson = null;
        HotkeyHint = prefixHint;
        NavigateFilePath = fullPath;
        NavigateLine = line;
        NavigateColumn = column;
        IsAvailable = true;
        UnavailableHint = null;
        IsMelodyAccentRow = false;
    }

    public IdeCommandPaletteRowViewModel(string title, string category)
    {
        RowKind = IdeCommandPaletteRowKind.Hint;
        PaletteId = "__hint__";
        CommandId = "__hint__";
        Title = title;
        Category = category;
        Subtitle = category;
        ArgsJson = null;
        HotkeyHint = null;
        IsAvailable = false;
        UnavailableHint = null;
        IsMelodyAccentRow = false;
    }

    /// <summary>
    /// Melody: команда исполняется через MCP, но нет строки в <see cref="IdeCommandPaletteCatalog"/> — заголовок из дока протокола.
    /// </summary>
    public IdeCommandPaletteRowViewModel(
        string commandId,
        string melodyAliasTail,
        string titleFromDoc,
        string? hotkeyHint,
        string? argsJson = null)
    {
        RowKind = IdeCommandPaletteRowKind.Command;
        PaletteId = commandId;
        CommandId = commandId;
        Title = $"c:{melodyAliasTail}";
        Category = "ide_execute_command";
        Subtitle = $"{titleFromDoc} · {commandId} · ide_execute_command";
        ArgsJson = argsJson;
        HotkeyHint = hotkeyHint;
        IsAvailable = true;
        UnavailableHint = null;
        IsMelodyAccentRow = true;
    }

    public IdeCommandPaletteRowKind RowKind { get; }

    public bool ShowUnavailableHint => RowKind == IdeCommandPaletteRowKind.Command && !IsAvailable && !string.IsNullOrEmpty(UnavailableHint);

    /// <summary>Строка из режима <c>c:</c> — увеличенный шрифт заголовка в палитре.</summary>
    public bool IsMelodyAccentRow { get; }

    /// <summary>Размер шрифта первой строки: чуть крупнее для мелодии <c>c:</c>.</summary>
    public double PaletteTitleFontSize => IsMelodyAccentRow ? 14.0 : 12.0;

    public string PaletteId { get; }
    public string CommandId { get; }
    public string Title { get; }
    /// <summary>Сырой тег группы из каталога (раздел палитры); для подписи в UI см. <see cref="Subtitle"/>.</summary>
    public string Category { get; }
    /// <summary>Вторая строка палитры: в обычном режиме <c>command_id · раздел</c>; в <c>c:</c> — подпись команды и id под мелодией.</summary>
    public string Subtitle { get; }
    public string? ArgsJson { get; }
    public string? HotkeyHint { get; }
    public bool IsAvailable { get; }
    public string? UnavailableHint { get; }
    public double RowOpacity => IsAvailable ? 1.0 : 0.45;

    public string? NavigateFilePath { get; }
    public int NavigateLine { get; }
    public int NavigateColumn { get; } = 1;
}

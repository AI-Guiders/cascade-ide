namespace CascadeIDE.IdeDisplay.CommandPalette;

/// <summary>
/// IDS v0: снимок поверхности оверлея палитры команд.
/// Отделяет данные рендера/навигации от конкретного UI-фреймворка.
/// </summary>
public sealed record CommandPaletteSurfaceSnapshot(
    string Query,
    int SelectedIndex,
    IReadOnlyList<CommandPaletteSurfaceEntry> Items)
{
    public static CommandPaletteSurfaceSnapshot Empty { get; } = new("", -1, []);
}

public sealed record CommandPaletteSurfaceEntry(
    string PaletteId,
    string CommandId,
    string Title,
    string Subtitle,
    string? HotkeyHint,
    bool IsAvailable,
    bool ShowUnavailableHint,
    string? UnavailableHint,
    double RowOpacity);

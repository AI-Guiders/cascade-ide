using CascadeIDE.ViewModels;

namespace CascadeIDE.IdeDisplay.CommandPalette;

/// <summary>Вход IDS-композитора для оверлея палитры команд (Ctrl+Q).</summary>
public readonly record struct CommandPaletteSurfaceIntent(
    string Query,
    int SelectedIndex,
    IReadOnlyList<IdeCommandPaletteRowViewModel> Rows);

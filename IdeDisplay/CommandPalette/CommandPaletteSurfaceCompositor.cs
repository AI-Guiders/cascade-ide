namespace CascadeIDE.IdeDisplay.CommandPalette;

/// <summary>
/// IDS: композитор поверхности палитры команд (канал → снимок для оверлея Ctrl+Q).
/// </summary>
public sealed class CommandPaletteSurfaceCompositor
    : IIdsSurfaceCompositor<CommandPaletteSurfaceIntent, CommandPaletteSurfaceSnapshot>
{
    public CommandPaletteSurfaceSnapshot Compose(CommandPaletteSurfaceIntent intent)
    {
        var query = intent.Query;
        var rows = intent.Rows;
        var selectedIndex = intent.SelectedIndex;

        if (rows.Count == 0)
            return new CommandPaletteSurfaceSnapshot(query, -1, []);

        var items = new List<CommandPaletteSurfaceEntry>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(new CommandPaletteSurfaceEntry(
                row.PaletteId,
                row.CommandId,
                row.Title,
                row.Subtitle,
                row.HotkeyHint,
                row.IsAvailable,
                row.ShowUnavailableHint,
                row.UnavailableHint,
                row.RowOpacity));
        }

        var normalizedSelected = selectedIndex < 0
            ? 0
            : Math.Min(selectedIndex, items.Count - 1);
        return new CommandPaletteSurfaceSnapshot(query, normalizedSelected, items);
    }
}

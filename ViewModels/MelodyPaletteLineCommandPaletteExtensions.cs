using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

/// <summary>C# 14 extension members: <see cref="MelodyPaletteLine"/> → строка палитры команд.</summary>
public static class MelodyPaletteLineCommandPaletteExtensions
{
    extension(MelodyPaletteLine line)
    {
        public IdeCommandPaletteRowViewModel? ToCommandPaletteRow(HotkeyGestureMap hotkeys, UiModeFamily family) =>
            line switch
            {
                MelodyPaletteHint hint => new IdeCommandPaletteRowViewModel(hint.Title, hint.Category),
                MelodyPaletteCommand cmd => IdeCommandPaletteCatalog.All.FirstOrDefault(e => e.CommandId == cmd.CommandId) is { } entry
                    ? new IdeCommandPaletteRowViewModel(entry, hotkeys.GetDisplayHint(entry.CommandId), family, cmd.Alias)
                    : null,
                _ => throw new InvalidOperationException($"Unknown melody line: {line.GetType().Name}"),
            };
    }
}

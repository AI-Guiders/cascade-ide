using CascadeIDE.Features.UiChrome;
using CascadeIDE.IdeDisplay.CommandPalette;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CommandPaletteSurfaceCompositorTests
{
    [Fact]
    public void Compose_EmptyRows_ReturnsEmptySnapshot()
    {
        var sut = new CommandPaletteSurfaceCompositor();
        var intent = new CommandPaletteSurfaceIntent("git", 0, []);
        var snapshot = sut.Compose(intent);

        Assert.Equal("git", snapshot.Query);
        Assert.Equal(-1, snapshot.SelectedIndex);
        Assert.Empty(snapshot.Items);
    }

    [Fact]
    public void Compose_MapsRowsAndClampsSelection()
    {
        var sut = new CommandPaletteSurfaceCompositor();
        var entry = IdeCommandPaletteCatalog.All[0];
        var rows = new List<IdeCommandPaletteRowViewModel>
        {
            new(entry, hotkeyHint: "Ctrl+Q", currentFamily: UiModeFamily.Balanced),
            new("Ничего не найдено", "Подсказка"),
        };

        var intent = new CommandPaletteSurfaceIntent("", 99, rows);
        var snapshot = sut.Compose(intent);

        Assert.Equal(1, snapshot.SelectedIndex);
        Assert.Equal(2, snapshot.Items.Count);
        Assert.Equal(rows[0].Title, snapshot.Items[0].Title);
        Assert.Equal(rows[0].Subtitle, snapshot.Items[0].Subtitle);
        Assert.Equal(rows[0].HotkeyHint, snapshot.Items[0].HotkeyHint);
    }
}

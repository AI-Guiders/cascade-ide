using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MelodyPaletteCatalogFallbackTests
{
    [Fact]
    public void MelodyPaletteCommand_ToRow_Uses_doc_when_command_not_in_palette_catalog()
    {
        Assert.DoesNotContain(
            IdeCommandPaletteCatalog.All,
            e => e.CommandId == "get_workspace_state");

        var line = new MelodyPaletteCommand("test_ws", "get_workspace_state");
        var row = line.ToCommandPaletteRow(HotkeyGestureMap.Load(), UiModeFamily.Balanced);
        Assert.NotNull(row);
        Assert.Equal("get_workspace_state", row!.CommandId);
        Assert.Contains("сводка", row.Title, StringComparison.OrdinalIgnoreCase);
    }
}

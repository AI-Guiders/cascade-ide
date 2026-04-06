using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using Xunit;
using System.Linq;

namespace CascadeIDE.Tests;

public sealed class IdeCommandPaletteMatchTests
{
    [Fact]
    public void FilterAndRank_EmptyQuery_SortsByCategoryThenTitle()
    {
        var list = IdeCommandPaletteMatch.FilterAndRank(IdeCommandPaletteCatalog.All, "");
        Assert.Equal(IdeCommandPaletteCatalog.All.Length, list.Count);
        var light = list.Select((e, i) => (e, i)).First(x => x.e.PaletteId == "apply_light_theme").i;
        var dark = list.Select((e, i) => (e, i)).First(x => x.e.PaletteId == "apply_dark_theme").i;
        Assert.True(light >= 0 && dark >= 0);
        Assert.True(light < dark, "В одной категории сортировка по заголовку");
    }

    [Fact]
    public void IsEntryAvailable_DebugCommands_OnlyInDebugFamily()
    {
        var cont = IdeCommandPaletteCatalog.All.First(e => e.PaletteId == "debug_continue");
        Assert.True(IdeCommandPaletteMatch.IsEntryAvailable(cont, UiModeFamily.Debug));
        Assert.False(IdeCommandPaletteMatch.IsEntryAvailable(cont, UiModeFamily.Balanced));
    }

    [Fact]
    public void FilterAndRank_FuzzyMatches_CommandId()
    {
        var list = IdeCommandPaletteMatch.FilterAndRank(IdeCommandPaletteCatalog.All, "gitst");
        Assert.Contains(list, e => e.PaletteId == "git_status");
    }
}

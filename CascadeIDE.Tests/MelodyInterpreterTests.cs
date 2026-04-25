using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MelodyInterpreterTests
{
    [Fact]
    public void BuildPalette_EmptyTail_StartsWithHint_AndSelectsFirstCommand()
    {
        var plan = MelodyInterpreter.BuildPalette("");
        Assert.True(plan.Lines[0] is MelodyPaletteHint);
        Assert.Contains(plan.Lines, l => l is MelodyPaletteCommand { Alias: "gs" });
        Assert.True(plan.Lines.Count >= 2);
        Assert.Equal(1, plan.SelectedIndex);
    }

    [Fact]
    public void BuildPalette_NoMatch_OneHint_SelectedZero()
    {
        var plan = MelodyInterpreter.BuildPalette("zzz");
        Assert.Single(plan.Lines);
        var h = Assert.IsType<MelodyPaletteHint>(plan.Lines[0]);
        Assert.Equal(MelodyInterpreter.NoMatchHintTitle, h.Title);
        Assert.Equal(0, plan.SelectedIndex);
    }

    [Fact]
    public void BuildPalette_Prefix_gs_Matches_gs_And_gsu_SelectedZero()
    {
        var plan = MelodyInterpreter.BuildPalette("gs");
        Assert.Equal(2, plan.Lines.Count);
        Assert.All(plan.Lines, l => Assert.IsType<MelodyPaletteCommand>(l));
        Assert.Equal(0, plan.SelectedIndex);
    }

    [Fact]
    public void BuildPalette_UnambiguousPrefix_br_OneCommand_SelectedZero()
    {
        var plan = MelodyInterpreter.BuildPalette("br");
        Assert.Single(plan.Lines);
        var c = Assert.IsType<MelodyPaletteCommand>(plan.Lines[0]);
        Assert.Equal("br", c.Alias);
        Assert.Equal(IdeCommands.BuildSolutionUi, c.CommandId);
        Assert.Equal(0, plan.SelectedIndex);
    }
}

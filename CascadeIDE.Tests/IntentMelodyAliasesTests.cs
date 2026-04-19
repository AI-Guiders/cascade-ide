using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntentMelodyAliasesTests
{
    [Theory]
    [InlineData("c:gs", "gs")]
    [InlineData("c: GS", "gs")]
    [InlineData("  c:br  ", "br")]
    [InlineData("c:", "")]
    public void TryGetTail_ParsesPrefix(string raw, string expectedTail) =>
        Assert.True(IntentMelodyAliases.TryGetTail(raw, out var tail) && tail == expectedTail);

    [Fact]
    public void TryGetTail_RejectsNonMelody() =>
        Assert.False(IntentMelodyAliases.TryGetTail("f:foo", out _));

    [Fact]
    public void TryResolveExact_gs_IsGitStatus() =>
        Assert.Equal(IdeCommands.GitStatus, IntentMelodyAliases.TryResolveExactCommandId("gs"));

    [Fact]
    public void TryResolveExact_so_IsOpenSolutionDialog() =>
        Assert.Equal(IdeCommands.OpenSolutionDialog, IntentMelodyAliases.TryResolveExactCommandId("so"));

    [Fact]
    public void FilterByTailPrefix_g_Matches_gs_gc_gp_gsu() =>
        Assert.Equal(4, IntentMelodyAliases.FilterByTailPrefix("g").Count);

    [Fact]
    public void HasStrictLongerAliasPrefix_gs_true_gsu_extends() =>
        Assert.True(IntentMelodyAliases.HasStrictLongerAliasPrefix("gs"));

    [Fact]
    public void HasStrictLongerAliasPrefix_so_false() =>
        Assert.False(IntentMelodyAliases.HasStrictLongerAliasPrefix("so"));
}

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
    public void FilterByTailPrefix_g_Matches_gs_gc_gp_gsu() =>
        Assert.Equal(4, IntentMelodyAliases.FilterByTailPrefix("g").Count);
}

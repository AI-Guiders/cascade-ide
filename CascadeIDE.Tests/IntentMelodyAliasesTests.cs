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
    public void TryResolveExact_dl_IsDebugLaunch() =>
        Assert.Equal(IdeCommands.DebugLaunch, IntentMelodyAliases.TryResolveExactCommandId("dl"));

    [Fact]
    public void TryResolveExact_so_IsOpenSolutionDialog() =>
        Assert.Equal(IdeCommands.OpenSolutionDialog, IntentMelodyAliases.TryResolveExactCommandId("so"));

    [Fact]
    public void TryResolveExact_chat_melodies_map_to_commands()
    {
        Assert.Equal(IdeCommands.ShowChatPage, IntentMelodyAliases.TryResolveExactCommandId("cps"));
        Assert.Equal(IdeCommands.SendChat, IntentMelodyAliases.TryResolveExactCommandId("cs"));
        Assert.Equal(IdeCommands.ChatExportReadable, IntentMelodyAliases.TryResolveExactCommandId("cex"));
        Assert.Equal(IdeCommands.ForkChatThread, IntentMelodyAliases.TryResolveExactCommandId("ctf"));
    }

    [Fact]
    public void TryResolveExact_environment_and_terminal_surface_melodies()
    {
        Assert.Equal(IdeCommands.ShowEnvironmentReadinessPage, IntentMelodyAliases.TryResolveExactCommandId("ers"));
        Assert.Equal(IdeCommands.ShowTerminalPanel, IntentMelodyAliases.TryResolveExactCommandId("ts"));
    }

    [Fact]
    public void HasStrictLongerAliasPrefix_er_true_ers_extends() =>
        Assert.True(IntentMelodyAliases.HasStrictLongerAliasPrefix("er"));

    [Fact]
    public void Bundled_intent_melody_toml_is_readable_like_runtime()
    {
        Assert.True(BundledAppContent.TryReadDiskThenEmbedded(IntentMelodyAliases.BundledRelativePath, out var text));
        Assert.Contains("[aliases]", text, StringComparison.Ordinal);
        Assert.Contains("ers = \"show_environment_readiness_page\"", text, StringComparison.Ordinal);
        Assert.Contains("ts = \"show_terminal_panel\"", text, StringComparison.Ordinal);
        Assert.Contains("dl = \"debug_launch\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HasStrictLongerAliasPrefix_ce_true_cex_extends() =>
        Assert.True(IntentMelodyAliases.HasStrictLongerAliasPrefix("ce"));

    [Fact]
    public void FilterByTailPrefix_c_Matches_cps_cs_cex_ctf() =>
        Assert.Equal(4, IntentMelodyAliases.FilterByTailPrefix("c").Count);

    [Fact]
    public void FilterByTailPrefix_g_Matches_gs_gc_gp_gsu() =>
        Assert.Equal(4, IntentMelodyAliases.FilterByTailPrefix("g").Count);

    [Fact]
    public void HasStrictLongerAliasPrefix_gs_true_gsu_extends() =>
        Assert.True(IntentMelodyAliases.HasStrictLongerAliasPrefix("gs"));

    [Fact]
    public void HasStrictLongerAliasPrefix_so_false() =>
        Assert.False(IntentMelodyAliases.HasStrictLongerAliasPrefix("so"));

    [Fact]
    public void SampleAliasesForFooter_lists_aliases()
    {
        var s = IntentMelodyAliases.SampleAliasesForFooter(4);
        Assert.False(string.IsNullOrWhiteSpace(s));
        Assert.Contains(",", s, StringComparison.Ordinal);
    }
}

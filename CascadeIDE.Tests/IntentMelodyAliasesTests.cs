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
    public void TryResolveExact_debug_step_melodies_map_to_dap_commands()
    {
        Assert.Equal(IdeCommands.DebugStepOver, IntentMelodyAliases.TryResolveExactCommandId("dn"));
        Assert.Equal(IdeCommands.DebugStepInto, IntentMelodyAliases.TryResolveExactCommandId("di"));
        Assert.Equal(IdeCommands.DebugStepOut, IntentMelodyAliases.TryResolveExactCommandId("df"));
        Assert.Equal(IdeCommands.DebugStop, IntentMelodyAliases.TryResolveExactCommandId("dx"));
    }

    [Fact]
    public void TryResolveExact_so_IsOpenSolutionDialog() =>
        Assert.Equal(IdeCommands.OpenSolutionDialog, IntentMelodyAliases.TryResolveExactCommandId("so"));

    [Fact]
    public void TryResolveExact_tol_IsToggleWorkspaceSplittersLock() =>
        Assert.Equal(IdeCommands.ToggleWorkspaceSplittersLock, IntentMelodyAliases.TryResolveExactCommandId("tol"));

    [Fact]
    public void TryResolveExact_chat_and_agent_melodies_map_to_commands()
    {
        Assert.Equal(IdeCommands.ShowChatPage, IntentMelodyAliases.TryResolveExactCommandId("cps"));
        Assert.Equal(IdeCommands.SendChat, IntentMelodyAliases.TryResolveExactCommandId("cs"));
        Assert.Equal(IdeCommands.ChatExportReadable, IntentMelodyAliases.TryResolveExactCommandId("cex"));
        Assert.Equal(IdeCommands.ForkChatThread, IntentMelodyAliases.TryResolveExactCommandId("ctf"));
        Assert.Equal(IdeCommands.ChatSelectPrevMessage, IntentMelodyAliases.TryResolveExactCommandId("amp"));
        Assert.Equal(IdeCommands.ChatSelectNextMessage, IntentMelodyAliases.TryResolveExactCommandId("amn"));
        Assert.Equal(IdeCommands.ChatToggleSelectedThinking, IntentMelodyAliases.TryResolveExactCommandId("amt"));
        Assert.Equal(IdeCommands.ChatToggleShowThinkingInHistory, IntentMelodyAliases.TryResolveExactCommandId("amh"));
        Assert.Equal(IdeCommands.ChatSelectPrevThread, IntentMelodyAliases.TryResolveExactCommandId("atp"));
        Assert.Equal(IdeCommands.ChatSelectNextThread, IntentMelodyAliases.TryResolveExactCommandId("atn"));
        Assert.Equal(IdeCommands.ChatOpenSelectedThread, IntentMelodyAliases.TryResolveExactCommandId("ato"));
        Assert.Equal(IdeCommands.ChatShowThreadOverview, IntentMelodyAliases.TryResolveExactCommandId("atb"));
    }

    [Fact]
    public void TryResolveExact_environment_and_terminal_surface_melodies()
    {
        Assert.Equal(IdeCommands.BuildSolutionUi, IntentMelodyAliases.TryResolveExactCommandId("br"));
        Assert.Equal(IdeCommands.BuildStructured, IntentMelodyAliases.TryResolveExactCommandId("bs"));
        Assert.Equal(IdeCommands.ShowEnvironmentReadinessPage, IntentMelodyAliases.TryResolveExactCommandId("ers"));
        Assert.Equal(IdeCommands.ShowHybridIndexPage, IntentMelodyAliases.TryResolveExactCommandId("his"));
        Assert.Equal(IdeCommands.ShowWebAiPortalPage, IntentMelodyAliases.TryResolveExactCommandId("wai"));
        Assert.Equal(IdeCommands.ShowTerminalPanel, IntentMelodyAliases.TryResolveExactCommandId("ts"));
        Assert.Equal(IdeCommands.Select, IntentMelodyAliases.TryResolveExactCommandId("els"));
        Assert.Equal(IdeCommands.ApplyEdit, IntentMelodyAliases.TryResolveExactCommandId("eld"));
    }

    [Fact]
    public void HasStrictLongerAliasPrefix_er_true_ers_extends() =>
        Assert.True(IntentMelodyAliases.HasStrictLongerAliasPrefix("er"));

    [Fact]
    public void Bundled_intent_melody_toml_is_readable_like_runtime()
    {
        Assert.True(BundledAppContent.TryReadDiskThenEmbedded(IntentMelodyAliases.BundledRelativePath, out var text));
        Assert.Contains("intent_catalog_schema_version = 1", text, StringComparison.Ordinal);
        Assert.Contains("[[tail_wire_class]]", text, StringComparison.Ordinal);
        Assert.Contains("[[command]]", text, StringComparison.Ordinal);
        Assert.Contains("[[command.form.slash]]", text, StringComparison.Ordinal);
        Assert.Contains("path = \"/terminal show\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"ers\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"show_environment_readiness_page\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"his\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"show_hybrid_index_page\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"wai\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_palette_hint_slug = \"wai-url\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"show_web_ai_portal_page\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"ts\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"show_terminal_panel\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"dl\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"debug_launch\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"dn\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"debug_step_over\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"df\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"debug_step_out\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"els\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"select\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"eld\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"apply_edit\"", text, StringComparison.Ordinal);
        Assert.Contains("melody_slug = \"tol\"", text, StringComparison.Ordinal);
        Assert.Contains("command_id = \"toggle_workspace_splitters_lock\"", text, StringComparison.Ordinal);
        Assert.Contains("slash_group = \"Панели\"", text, StringComparison.Ordinal);
        Assert.Contains("[command.form.slash.args]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseBundle_command_model_flat_melody_and_form_slash()
    {
        const string toml =
            """
            intent_catalog_schema_version = 1
            [[command]]
            command_id = "git_status"
            melody_slug = "gs"
            melody_shape = "simple"
            [[command.form.slash]]
            path = "/git status"
            help = "git status"
            """;

        var bundle = IntentMelodyAliases.ParseBundleForTests(toml.Trim());

        Assert.Equal(IdeCommands.GitStatus, IntentMelodyAliases.TryResolveExactCommandId("gs"));
        Assert.True(bundle.Catalog.SlashRoutes.TryGetValue("/git status", out var slash));
        Assert.Equal(IdeCommands.GitStatus, slash.CommandId);
    }

    [Fact]
    public void Tomlyn_deserializes_between_slots_two_single_char_literals()
    {
        const string minimal =
            """
            intent_catalog_schema_version = 1

            [[tail_wire_class]]
            id = "int_chain_colon_space"
            kind = "delimited_slots"
            between_slots_any_of = [":", " "]

            [[command]]
            command_id = "git_status"
            [command.melody]
            slug = "z"
            shape = "simple"
            """;

        var parsed = CascadeTomlSerializer.Deserialize<IntentMelodyAliases.IntentMelodyTomlRoot>(minimal.Trim());
        Assert.NotNull(parsed?.TailWireClass);
        Assert.Single(parsed.TailWireClass);
        var row = parsed.TailWireClass[0];
        Assert.NotNull(row.BetweenSlotsAnyOf);
        Assert.Equal(2, row.BetweenSlotsAnyOf!.Length);
        Assert.Equal(":", row.BetweenSlotsAnyOf![0]);
        Assert.Single(row.BetweenSlotsAnyOf![1]!);
        Assert.Equal(' ', row.BetweenSlotsAnyOf![1]![0]);

        IntentMelodyAliases.ParseBundleForTests(minimal.Trim());
    }

    [Fact]
    public void ParseBundle_command_model_melody_and_slash_forms()
    {
        const string toml =
            """
            intent_catalog_schema_version = 1
            [[command]]
            command_id = "git_status"
            [command.melody]
            slug = "gs"
            shape = "simple"
            [[command.slash]]
            path = "/git status"
            help = "git status"
            """;

        var bundle = IntentMelodyAliases.ParseBundleForTests(toml.Trim());

        Assert.Equal(IdeCommands.GitStatus, IntentMelodyAliases.TryResolveExactCommandId("gs"));
        Assert.True(bundle.Catalog.SlashRoutes.TryGetValue("/git status", out var slash));
        Assert.Equal(IdeCommands.GitStatus, slash.CommandId);
    }

    [Fact]
    public void ParseBundle_infers_show_usage_hint_for_two_int_parametric_without_explicit_flag()
    {
        const string toml =
            """
            intent_catalog_schema_version = 1

            [[tail_wire_class]]
            id = "int_chain_colon_space"
            kind = "delimited_slots"
            between_slots_any_of = [":", " "]

            [[command]]
            command_id = "select"
            [command.melody]
            slug = "zz"
            shape = "parametric"
            tail_signature = "<start:ln>:<end:ln>"
            wire_class = "int_chain_colon_space"
            chord_commit = "enter"
            """;

        var bundle = IntentMelodyAliases.ParseBundleForTests(toml.Trim());

        Assert.True(bundle.Catalog.Roots.TryGetValue("zz", out var z));
        Assert.True(z.ShowUsageHintIfBareSlug);
    }

    /// <summary>При локальном <c>dotnet test</c> полный проход <see cref="IntentMelodyAliases.Build"/> по embedded TOML (без дискового оверлея в <c>bin/</c>).</summary>
    [Fact]
    public void Embedded_intent_melody_ParseBundle_Build_succeeds()
    {
        Assert.True(BundledAppContent.TryReadEmbeddedText(IntentMelodyAliases.BundledRelativePath, out var text));
        var bundle = IntentMelodyAliases.ParseBundleForTests(text.Trim());

        Assert.NotEmpty(bundle.AliasToCommandId);
        Assert.NotEmpty(bundle.Catalog.Roots);
        Assert.NotEmpty(bundle.Catalog.TailWireClasses);
        Assert.NotEmpty(bundle.Catalog.SlashRoutes);
        Assert.True(bundle.Catalog.SlashRoutes.TryGetValue("/terminal show", out var term));
        Assert.Equal(IdeCommands.SetMfdShellPage, term.CommandId);
        Assert.Equal("Terminal", term.MfdPage);
        Assert.Contains(bundle.Catalog.Roots.Values, static e => e.Shape == IntentMelodyShape.Parametric);
        Assert.True(bundle.Catalog.Roots["els"].ShowUsageHintIfBareSlug, "els — два int-слота, подсказка по умолчанию");
        Assert.False(bundle.Catalog.Roots["wai"].ShowUsageHintIfBareSlug, "wai — явно false в TOML");
    }

    [Fact]
    public void HasStrictLongerAliasPrefix_ce_true_cex_extends() =>
        Assert.True(IntentMelodyAliases.HasStrictLongerAliasPrefix("ce"));

    [Fact]
    public void FilterByTailPrefix_c_Matches_chat_core_melodies() =>
        Assert.Equal(4, IntentMelodyAliases.FilterByTailPrefix("c").Count);

    [Fact]
    public void FilterByTailPrefix_a_Matches_agent_navigation_melodies() =>
        Assert.Equal(8, IntentMelodyAliases.FilterByTailPrefix("a").Count);

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

#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Code-side verify for Glass Ctrl+Q / Ctrl+K catalogs (cabin down dogfood peel).</summary>
public sealed class GlassPaletteChordCatalogTests
{
    [Fact]
    public void Palette_Filter_empty_returns_all_entries()
    {
        var all = GlassCommandPaletteCatalog.Filter(null);
        Assert.NotEmpty(all);
        Assert.Equal(all.Count, GlassCommandPaletteCatalog.Filter("").Count);
        Assert.Equal(all.Count, GlassCommandPaletteCatalog.Filter("   ").Count);
    }

    [Fact]
    public void Palette_Filter_matches_id_title_help_or_keywords()
    {
        Assert.Contains(GlassCommandPaletteCatalog.Filter("mfd_editor"), e => e.Id == "mfd_editor");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("Open file"), e => e.Id == "open_file");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("ctrl+o"), e => e.Id == "open_file");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("AvalonEdit"), e => e.Id == "open_file");
        Assert.DoesNotContain(GlassCommandPaletteCatalog.Filter("zzzz-no-hit"), e => true);
    }

    [Fact]
    public void Chord_Normalize_strips_non_alnum_and_lowercases()
    {
        Assert.Equal("pq", GlassChordCatalog.Normalize("PQ"));
        Assert.Equal("pq", GlassChordCatalog.Normalize(" P-Q "));
        Assert.Equal("", GlassChordCatalog.Normalize("---"));
        Assert.Equal("", GlassChordCatalog.Normalize(null));
    }

    [Fact]
    public void Chord_Filter_is_alias_prefix_only()
    {
        var pq = GlassChordCatalog.Filter("p");
        Assert.Contains(pq, e => e.Alias == "pq");
        // Title search belongs to Ctrl+Q — "Open" must not match chord aliases.
        Assert.Empty(GlassChordCatalog.Filter("Open"));
    }

    [Fact]
    public void Chord_Exact_and_palette_bridge()
    {
        var exact = GlassChordCatalog.Exact("pq");
        Assert.NotNull(exact);
        Assert.Equal("palette", exact!.ActionId);

        Assert.Null(GlassChordCatalog.Exact("p")); // prefix only, not exact
        Assert.Null(GlassChordCatalog.Exact("nope"));
    }

    [Fact]
    public void Chord_aliases_cover_palette_action_ids()
    {
        var chordActions = GlassChordCatalog.Filter("")
            .Select(e => e.ActionId)
            .Where(id => id != "palette")
            .ToHashSet(StringComparer.Ordinal);
        var paletteIds = GlassCommandPaletteCatalog.Filter("")
            .Select(e => e.Id)
            .ToHashSet(StringComparer.Ordinal);

        var missing = chordActions.Where(id => !paletteIds.Contains(id)).ToArray();
        Assert.True(
            missing.Length == 0,
            "Chord ActionId without palette entry: " + string.Join(", ", missing));
    }

    [Fact]
    public void Chord_Exact_lived_aliases_fds_attach_open()
    {
        Assert.Equal("mfd_fds", GlassChordCatalog.Exact("fd")!.ActionId);
        Assert.Equal("slash_attach", GlassChordCatalog.Exact("at")!.ActionId);
        Assert.Equal("slash_open", GlassChordCatalog.Exact("op")!.ActionId);
        Assert.Equal("mfd_build", GlassChordCatalog.Exact("mb")!.ActionId);
        Assert.Equal("mfd_tests", GlassChordCatalog.Exact("ms")!.ActionId);
        Assert.Equal("mfd_git", GlassChordCatalog.Exact("mg")!.ActionId);
        Assert.Equal("slash_citizen", GlassChordCatalog.Exact("cz")!.ActionId);
        Assert.Equal("mfd_solution_explorer", GlassChordCatalog.Exact("sx")!.ActionId);
        Assert.Equal("mfd_hybrid_index", GlassChordCatalog.Exact("hi")!.ActionId);
        Assert.Equal("mfd_workspace_health", GlassChordCatalog.Exact("wh")!.ActionId);
        Assert.Equal("mfd_env_ready", GlassChordCatalog.Exact("er")!.ActionId);
        Assert.Equal("mfd_events", GlassChordCatalog.Exact("ev")!.ActionId);
        Assert.Equal("mfd_hypotheses", GlassChordCatalog.Exact("hy")!.ActionId);
        Assert.Equal("mfd_chat", GlassChordCatalog.Exact("ic")!.ActionId);
    }

    [Fact]
    public void Palette_includes_fds_attach_and_mfd_pages()
    {
        Assert.Contains(GlassCommandPaletteCatalog.Filter("fds"), e => e.Id is "slash_fds" or "mfd_fds");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("attach"), e => e.Id == "slash_attach");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("mfd_build"), e => e.Id == "mfd_build");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("mfd_git"), e => e.Id == "mfd_git");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("citizen"), e => e.Id == "slash_citizen");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("solution"), e => e.Id == "mfd_solution_explorer");
        Assert.Contains(GlassCommandPaletteCatalog.Filter("hypotheses"), e => e.Id == "mfd_hypotheses");
    }

    [Fact]
    public void Melody_TryGetTail_parses_c_prefix()
    {
        Assert.True(GlassCommandPaletteCatalog.TryGetMelodyTail("c:", out var empty) && empty == "");
        Assert.True(GlassCommandPaletteCatalog.TryGetMelodyTail("C:gs", out var gs) && gs == "gs");
        Assert.True(GlassCommandPaletteCatalog.TryGetMelodyTail("  c: br ", out var br) && br == "br");
        Assert.False(GlassCommandPaletteCatalog.TryGetMelodyTail("f:foo", out _));
        Assert.False(GlassCommandPaletteCatalog.TryGetMelodyTail("open", out _));
    }

    [Fact]
    public void Melody_Filter_empty_tail_lists_intent_catalog_aliases_with_help()
    {
        var rows = GlassCommandPaletteCatalog.Filter("c:");
        Assert.NotEmpty(rows);
        Assert.Equal(GlassCommandPaletteCatalog.MelodyHintId, rows[0].Id);
        Assert.Contains("Command Melody", rows[0].Title, StringComparison.Ordinal);
        Assert.True(GlassIntentMelodyCatalog.All().Count > 0, "intent-catalog.toml must load (embed/disk)");
        Assert.Contains(rows, e => e.Keywords == "gs" && e.Help.Length > 0);
        Assert.Contains(rows, e => e.Id == GlassIntentMelodyCatalog.ToRowId("git_status"));
        Assert.Contains(rows, e => e.Keywords == "of"); // c:of → open_file_dialog allowlist
        Assert.Contains(rows, e => e.Keywords == "fe"); // c:fe → focus_editor
    }

    [Fact]
    public void Melody_Filter_prefix_and_no_match()
    {
        var gs = GlassCommandPaletteCatalog.Filter("c:gs");
        Assert.DoesNotContain(gs, e => e.Id == GlassCommandPaletteCatalog.MelodyHintId);
        Assert.Contains(gs, e => e.Keywords == "gs");
        Assert.Contains(gs, e => e.Keywords == "gsu"); // prefix

        var miss = GlassCommandPaletteCatalog.Filter("c:zzzz");
        Assert.Single(miss);
        Assert.Equal(GlassCommandPaletteCatalog.MelodyNoMatchId, miss[0].Id);
    }

    [Fact]
    public void Melody_catalog_rows_allowlist_exec_vs_browse()
    {
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("git_status", out var git)
                    && git == GlassMelodyGlassActions.RunGitStatus);
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("build_solution_ui", out var br)
                    && br == GlassMelodyGlassActions.RunBuild);
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("run_tests", out var bt)
                    && bt == GlassMelodyGlassActions.RunTests);
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("select", out var sel)
                    && sel == GlassMelodyGlassActions.RunSelectLines);

        var gsRow = GlassIntentMelodyCatalog.ToRowId("git_status");
        Assert.False(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(gsRow));

        var unknown = GlassIntentMelodyCatalog.ToRowId("debug_attach");
        Assert.True(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(unknown));
        Assert.False(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow("open_file"));
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("open_file_dialog", out var of)
                    && of == "open_file");
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("open_file", out var of2)
                    && of2 == "open_file");
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("intercom.attach_selection", out var ias)
                    && ias == "slash_attach");
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("intercom.attach_scope", out var isc)
                    && isc == "slash_attach");
        Assert.Contains(GlassIntentMelodyCatalog.All(), a => a.Alias == "of");
        Assert.Contains(GlassIntentMelodyCatalog.All(), a => a.Alias == "fe");

    }

    [Fact]
    public void Melody_Tail_parses_parametric_line_range()
    {
        Assert.Equal("els", GlassMelodyTail.AliasPrefix("els:10:20"));
        Assert.Equal("10:20", GlassMelodyTail.ArgRemainder("els:10:20"));
        Assert.Equal("10;20", GlassMelodyTail.ArgRemainder("els;10;20"));
        Assert.True(GlassMelodyTail.TryParseLineRange("40", out var one, out var oneEnd) && one == 40 && oneEnd is null);
        Assert.True(GlassMelodyTail.TryParseLineRange("10:20", out var a, out var b) && a == 10 && b == 20);
        Assert.True(GlassMelodyTail.TryParseLineRange("10;20", out var c, out var d) && c == 10 && d == 20);
        Assert.False(GlassMelodyTail.TryParseLineRange("", out _, out _));

        var rows = GlassCommandPaletteCatalog.Filter("c:els:10");
        Assert.Contains(rows, e => e.Keywords == "els");
        Assert.False(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(
            GlassIntentMelodyCatalog.ToRowId("select")));
    }
    [Fact]
    public void Status_IOP_glance_format_includes_editor_mfd_topology()
    {
        var body = GlassIopStatusGlance.Format(new GlassIopStatusGlance.Snapshot(
            WorkspaceRoot: "D:/ws",
            IntercomForward: true,
            StatusLine: "glass ok",
            Subtitle: "peer",
            EditorPath: "D:/ws/MainWindow.cs",
            CaretLine: 42,
            EditorDirty: true,
            MfdPage: "Editor",
            Topology: "integrated",
            ColumnDefinitions: "* * *",
            LatchStateRoot: "D:/ws/.cdp/latches"));

        Assert.Contains("workspace: D:/ws", body, StringComparison.Ordinal);
        Assert.Contains("editor: D:/ws/MainWindow.cs", body, StringComparison.Ordinal);
        Assert.Contains("caret: 42", body, StringComparison.Ordinal);
        Assert.Contains("dirty: yes", body, StringComparison.Ordinal);
        Assert.Contains("mfd: Editor", body, StringComparison.Ordinal);
        Assert.Contains("topology: integrated", body, StringComparison.Ordinal);
        Assert.Contains("latch:", body, StringComparison.Ordinal);
        Assert.Contains("intercom forward: True", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Melody_get_ide_state_maps_to_slash_status_and_c_st()
    {
        Assert.True(GlassMelodyGlassActions.TryMapCommandId("get_ide_state", out var action)
                    && action == "slash_status");
        Assert.False(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(
            GlassIntentMelodyCatalog.ToRowId("get_ide_state")));

        var rows = GlassCommandPaletteCatalog.Filter("c:st");
        Assert.Contains(rows, e => e.Keywords == "st");
        Assert.Contains(rows, e => e.Id == GlassIntentMelodyCatalog.ToRowId("get_ide_state"));
    }

}

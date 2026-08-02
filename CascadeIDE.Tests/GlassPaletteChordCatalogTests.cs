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
        Assert.DoesNotContain(rows, e => e.Keywords == "of"); // GlassChord-only peel retired for c:
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
    public void Melody_catalog_rows_are_non_executable_discoverability()
    {
        Assert.True(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(
            GlassIntentMelodyCatalog.ToRowId("git_status")));
        Assert.False(GlassCommandPaletteCatalog.IsNonExecutableMelodyRow("open_file"));
    }

}

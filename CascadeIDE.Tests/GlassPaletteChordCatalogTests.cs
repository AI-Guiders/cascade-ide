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
}

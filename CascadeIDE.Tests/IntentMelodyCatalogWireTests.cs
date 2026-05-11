using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntentMelodyCatalogWireTests
{
    [Fact]
    public void ParseBundle_unknown_wire_class_throws()
    {
        const string toml =
            """
            melody_catalog_schema_version = 1
            [[tail_wire_class]]
            id = "ok"
            kind = "delimited_slots"
            between_slots_any_of = [":"]
            [[melody_root]]
            slug = "x"
            command_id = "select"
            shape = "parametric"
            tail_signature = "<start:int>:<end:int>"
            wire_class = "missing"
            chord_commit = "enter"
            """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            IntentMelodyAliases.ParseBundleForTests(toml.Trim()));

        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseBundle_wire_kind_mismatch_delimited_requires_two_int_slots()
    {
        const string toml =
            """
            melody_catalog_schema_version = 1
            [[tail_wire_class]]
            id = "del"
            kind = "delimited_slots"
            between_slots_any_of = [":"]
            [[melody_root]]
            slug = "x"
            command_id = "cmd"
            shape = "parametric"
            tail_signature = "<url:url>"
            wire_class = "del"
            chord_commit = "enter"
            """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            IntentMelodyAliases.ParseBundleForTests(toml.Trim()));

        Assert.Contains("DelimitedSlots", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseBundle_wire_kind_mismatch_single_remainder_requires_url_slot()
    {
        const string toml =
            """
            melody_catalog_schema_version = 1
            [[tail_wire_class]]
            id = "sr"
            kind = "single_remainder"
            [[melody_root]]
            slug = "x"
            command_id = "cmd"
            shape = "parametric"
            tail_signature = "<a:int>:<b:int>"
            wire_class = "sr"
            chord_commit = "enter"
            """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            IntentMelodyAliases.ParseBundleForTests(toml.Trim()));

        Assert.Contains("SingleRemainder", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ChordDefersInstantExecute_respects_enter_vs_instant_commit()
    {
        var enterCommit = new MelodyRootEntry(
            "p",
            "cmd",
            IntentMelodyShape.Parametric,
            ShowUsageHintIfBareSlug: false,
            TailSignature: "<start:ln>:<end:ln>",
            WireClass: "int_chain_colon_space",
            ChordCommit: "enter",
            PaletteHintSlug: null);

        var instantCommit = enterCommit with { ChordCommit = "instant" };

        Assert.True(ParametricIntentMelody.ChordDefersInstantExecuteForMelodyRoot(enterCommit));
        Assert.False(ParametricIntentMelody.ChordDefersInstantExecuteForMelodyRoot(instantCommit));
    }

    [Fact]
    public void ResolvePaletteHintKey_falls_back_to_slug_when_field_absent()
    {
        Assert.Equal("gs", ParametricIntentMelody.ResolvePaletteHintKey("gs"));
    }

    [Fact]
    public void ResolvePaletteHintKey_uses_bundled_palette_hint_slug_for_wai()
    {
        Assert.Equal("wai-url", ParametricIntentMelody.ResolvePaletteHintKey("wai"));
    }
}

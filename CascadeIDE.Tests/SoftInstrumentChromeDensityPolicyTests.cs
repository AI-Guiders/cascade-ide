#nullable enable

using CascadeIDE.Features.UiChrome;
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftInstrumentChromeDensityPolicyTests
{
    [Theory]
    [InlineData("pressure")]
    [InlineData("alert")]
    [InlineData("qrh")]
    [InlineData("review")]
    [InlineData("sa_desk")]
    [InlineData("sa-desk")]
    public void Avalonia_facade_priority_matches_GlassCore(string id) =>
        Assert.Equal(
            SoftInstrumentChromeDensityPolicy.PriorityFor(id),
            AgentChromeHintDensityPolicy.PriorityFor(id));

    [Fact]
    public void Collapse_labels_match_across_hosts()
    {
        Assert.Equal(
            SoftInstrumentChromeDensityPolicy.CollapseLabel,
            SoftInstrumentChromeAggregator.CollapseLabel);
        Assert.Equal(
            SoftInstrumentChromeDensityPolicy.CollapseLabel,
            AgentChromeHintDensityPolicy.CollapseLabel);
    }

    [Theory]
    [InlineData("pressure-LATEST.json", "pressure")]
    [InlineData("sa-desk-LATEST.json", SoftInstrumentLatchCatalog.SaDesk)]
    [InlineData("sa_desk-LATEST.json", SoftInstrumentLatchCatalog.SaDesk)]
    [InlineData("SA-DESK-LATEST.json", SoftInstrumentLatchCatalog.SaDesk)]
    public void LatchCatalog_parses_known_stems(string fileName, string expectedId)
    {
        Assert.True(SoftInstrumentLatchCatalog.TryParseFileName(fileName, out var id));
        Assert.Equal(expectedId, id, StringComparer.OrdinalIgnoreCase);
        Assert.True(SoftInstrumentLatchCatalog.Contains(id));
    }

    [Fact]
    public void LatchCatalog_rejects_unknown_and_non_latch()
    {
        Assert.False(SoftInstrumentLatchCatalog.TryParseFileName("unknown-LATEST.json", out _));
        Assert.False(SoftInstrumentLatchCatalog.TryParseFileName("pressure.json", out _));
        Assert.False(SoftInstrumentLatchCatalog.Contains("not-an-organ"));
    }

    [Fact]
    public void LatchCatalog_Canonicalize_maps_legacy_sa_desk()
    {
        Assert.Equal(SoftInstrumentLatchCatalog.SaDesk, SoftInstrumentLatchCatalog.Canonicalize("sa_desk"));
        Assert.Equal(SoftInstrumentLatchCatalog.SaDesk, SoftInstrumentLatchCatalog.Canonicalize("SA_DESK"));
        Assert.Equal(SoftInstrumentLatchCatalog.SaDesk, SoftInstrumentLatchCatalog.Canonicalize("sa-desk"));
        Assert.True(SoftInstrumentLatchCatalog.Contains("sa_desk"));
        Assert.Equal("test_desk", SoftInstrumentLatchCatalog.Canonicalize("test"));
        Assert.Equal("test_desk", SoftInstrumentLatchCatalog.Canonicalize("test_sa"));
        Assert.True(SoftInstrumentLatchCatalog.Contains("test"));
        Assert.Equal("debug_desk", SoftInstrumentLatchCatalog.Canonicalize("debug"));
        Assert.Equal("debug_desk", SoftInstrumentLatchCatalog.Canonicalize("dap_sa"));
        Assert.True(SoftInstrumentLatchCatalog.Contains("debug_desk"));
    }

    [Fact]
    public void Aggregator_Apply_merges_sa_desk_alias_into_canonical()
    {
        var agg = new SoftInstrumentChromeAggregator();
        agg.Apply("sa_desk", "from underscore");
        agg.Apply(SoftInstrumentLatchCatalog.SaDesk, "from hyphen");
        var band = agg.Snapshot();
        Assert.Single(band.VisibleLines);
        Assert.Equal("from hyphen", band.VisibleLines[0]);
    }

    /// <summary>Keep in sync with CollectChromeHintCandidates seats in SoftInstrumentChrome.</summary>
    [Fact]
    public void Density_From_canonicalizes_legacy_sa_desk_id()
    {
        var h = SoftInstrumentChromeDensityPolicy.From("sa_desk", "hint");
        Assert.NotNull(h);
        Assert.Equal(SoftInstrumentLatchCatalog.SaDesk, h.Value.Id);
        Assert.Equal(27, h.Value.Priority);

        var facade = AgentChromeHintDensityPolicy.From("SA_DESK", "hint");
        Assert.NotNull(facade);
        Assert.Equal(SoftInstrumentLatchCatalog.SaDesk, facade.Value.Id);
        Assert.Equal(27, facade.Value.Priority);
    }

    [Fact]
    public void Avalonia_seat_ids_are_catalog_members()
    {
        string[] avaloniaSeats =
        [
            "pressure", "ignite", "plan", SoftInstrumentLatchCatalog.Hands, "cabin", "scope", "review", "refactor", "plugins",
            "toolchain", "test_desk", "debug_desk", "build_desk", "files_desk", "find_desk", "crm", "report", "webcam", "sys", "onboard", "arch", "mcp", "learn", "domain",
            "md_author", "rules", "calendar", "fdr", "teeth", "postmortem", "glass", "problems",
            SoftInstrumentLatchCatalog.SaDesk,
        ];

        Assert.All(avaloniaSeats, id => Assert.True(SoftInstrumentLatchCatalog.Contains(id), id));
        Assert.Equal(
            SoftInstrumentLatchCatalog.Ids.Count,
            avaloniaSeats.Length);
        Assert.True(
            SoftInstrumentLatchCatalog.Ids.ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(avaloniaSeats));
    }

    [Fact]
    public void Hands_organ_is_face_receipt_chip()
    {
        Assert.True(SoftInstrumentLatchCatalog.Contains("hands"));
        Assert.Equal(SoftInstrumentLatchCatalog.Hands, SoftInstrumentLatchCatalog.Canonicalize("cdp_hands"));
        Assert.Equal("HND", SoftInstrumentChromeDensityPolicy.ShortLabel(SoftInstrumentLatchCatalog.Hands));
        Assert.Equal(3, SoftInstrumentChromeDensityPolicy.PriorityFor(SoftInstrumentLatchCatalog.Hands));
        Assert.Equal(GlassChipLevel.Caution, SoftInstrumentChromeDensityPolicy.ChipLevelFromHint("CAUTION · RUNNING · 12s"));
        Assert.Equal(GlassChipLevel.Fail, SoftInstrumentChromeDensityPolicy.ChipLevelFromHint("FAIL · kb · 12s"));
        Assert.Equal(GlassChipLevel.Quiet, SoftInstrumentChromeDensityPolicy.ChipLevelFromHint("OK · kb · 12s"));
    }
}

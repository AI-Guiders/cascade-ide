#nullable enable

using CascadeIDE.Features.UiChrome;
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftOrganChromeDensityPolicyTests
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
            SoftOrganChromeDensityPolicy.PriorityFor(id),
            AgentChromeHintDensityPolicy.PriorityFor(id));

    [Fact]
    public void Collapse_labels_match_across_hosts()
    {
        Assert.Equal(
            SoftOrganChromeDensityPolicy.CollapseLabel,
            SoftOrganChromeAggregator.CollapseLabel);
        Assert.Equal(
            SoftOrganChromeDensityPolicy.CollapseLabel,
            AgentChromeHintDensityPolicy.CollapseLabel);
    }

    [Theory]
    [InlineData("pressure-LATEST.json", "pressure")]
    [InlineData("sa-desk-LATEST.json", SoftOrganLatchCatalog.SaDesk)]
    [InlineData("sa_desk-LATEST.json", SoftOrganLatchCatalog.SaDesk)]
    [InlineData("SA-DESK-LATEST.json", SoftOrganLatchCatalog.SaDesk)]
    public void LatchCatalog_parses_known_stems(string fileName, string expectedId)
    {
        Assert.True(SoftOrganLatchCatalog.TryParseFileName(fileName, out var id));
        Assert.Equal(expectedId, id, StringComparer.OrdinalIgnoreCase);
        Assert.True(SoftOrganLatchCatalog.Contains(id));
    }

    [Fact]
    public void LatchCatalog_rejects_unknown_and_non_latch()
    {
        Assert.False(SoftOrganLatchCatalog.TryParseFileName("unknown-LATEST.json", out _));
        Assert.False(SoftOrganLatchCatalog.TryParseFileName("pressure.json", out _));
        Assert.False(SoftOrganLatchCatalog.Contains("not-an-organ"));
    }

    [Fact]
    public void LatchCatalog_Canonicalize_maps_legacy_sa_desk()
    {
        Assert.Equal(SoftOrganLatchCatalog.SaDesk, SoftOrganLatchCatalog.Canonicalize("sa_desk"));
        Assert.Equal(SoftOrganLatchCatalog.SaDesk, SoftOrganLatchCatalog.Canonicalize("SA_DESK"));
        Assert.Equal(SoftOrganLatchCatalog.SaDesk, SoftOrganLatchCatalog.Canonicalize("sa-desk"));
        Assert.True(SoftOrganLatchCatalog.Contains("sa_desk"));
        Assert.Equal("test_desk", SoftOrganLatchCatalog.Canonicalize("test"));
        Assert.Equal("test_desk", SoftOrganLatchCatalog.Canonicalize("test_sa"));
        Assert.True(SoftOrganLatchCatalog.Contains("test"));
        Assert.Equal("debug_desk", SoftOrganLatchCatalog.Canonicalize("debug"));
        Assert.Equal("debug_desk", SoftOrganLatchCatalog.Canonicalize("dap_sa"));
        Assert.True(SoftOrganLatchCatalog.Contains("debug_desk"));
    }

    [Fact]
    public void Aggregator_Apply_merges_sa_desk_alias_into_canonical()
    {
        var agg = new SoftOrganChromeAggregator();
        agg.Apply("sa_desk", "from underscore");
        agg.Apply(SoftOrganLatchCatalog.SaDesk, "from hyphen");
        var band = agg.Snapshot();
        Assert.Single(band.VisibleLines);
        Assert.Equal("from hyphen", band.VisibleLines[0]);
    }

    /// <summary>Keep in sync with CollectChromeHintCandidates seats in SoftOrganChrome.</summary>
    [Fact]
    public void Density_From_canonicalizes_legacy_sa_desk_id()
    {
        var h = SoftOrganChromeDensityPolicy.From("sa_desk", "hint");
        Assert.NotNull(h);
        Assert.Equal(SoftOrganLatchCatalog.SaDesk, h.Value.Id);
        Assert.Equal(24, h.Value.Priority);

        var facade = AgentChromeHintDensityPolicy.From("SA_DESK", "hint");
        Assert.NotNull(facade);
        Assert.Equal(SoftOrganLatchCatalog.SaDesk, facade.Value.Id);
        Assert.Equal(24, facade.Value.Priority);
    }

    [Fact]
    public void Avalonia_seat_ids_are_catalog_members()
    {
        string[] avaloniaSeats =
        [
            "pressure", "ignite", "plan", "cabin", "scope", "review", "refactor", "plugins",
            "toolchain", "test_desk", "debug_desk", "files_desk", "crm", "report", "webcam", "sys", "onboard", "arch", "mcp", "learn", "domain",
            SoftOrganLatchCatalog.SaDesk,
        ];

        Assert.All(avaloniaSeats, id => Assert.True(SoftOrganLatchCatalog.Contains(id), id));
        Assert.Equal(
            SoftOrganLatchCatalog.Ids.Count,
            avaloniaSeats.Length);
        Assert.True(
            SoftOrganLatchCatalog.Ids.ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(avaloniaSeats));
    }
}

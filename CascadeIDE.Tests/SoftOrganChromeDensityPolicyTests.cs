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
    [InlineData("sa-desk-LATEST.json", "sa-desk")]
    [InlineData("SA-DESK-LATEST.json", "SA-DESK")]
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
}

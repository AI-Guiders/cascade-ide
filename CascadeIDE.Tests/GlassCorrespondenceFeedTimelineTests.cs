using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassCorrespondenceFeedTimelineTests
{
    [Fact]
    public void BuildTimeline_orders_reverse_then_focus_then_forward()
    {
        var snap = new GlassCorrespondenceFeed.Snapshot(
            Reverse:
            [
                new GlassCorrespondenceFeed.Item(@"C:\ws\docs\a.md", "anchor", 12, "A"),
            ],
            Forward:
            [
                new GlassCorrespondenceFeed.Item(@"C:\ws\docs\b.md", "adr", Title: "B"),
                new GlassCorrespondenceFeed.Item(@"C:\ws\docs\c.md", "feature", Title: "C"),
            ],
            StatusLine: "crs · test",
            FeatureLine: "feat · demo",
            AdrLine: "adr · 1",
            DocsCoverageLine: "",
            LayersBadge: "L1");

        var focus = Path.GetTempFileName();
        try
        {
            var rows = GlassCorrespondenceFeed.BuildTimeline(snap, focus);
            Assert.Equal(4, rows.Count);
            Assert.Equal("reverse", rows[0].Role);
            Assert.Equal("focus", rows[1].Role);
            Assert.Equal("forward", rows[2].Role);
            Assert.Equal("forward", rows[3].Role);
            Assert.StartsWith("◀", rows[0].Display, StringComparison.Ordinal);
            Assert.StartsWith("◆", rows[1].Display, StringComparison.Ordinal);
            Assert.StartsWith("▶", rows[2].Display, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(focus);
        }
    }

    [Fact]
    public void BuildInstrument_emits_six_cards()
    {
        var snap = new GlassCorrespondenceFeed.Snapshot(
            Reverse: [new GlassCorrespondenceFeed.Item(@"C:\ws\a.md", "anchor")],
            Forward: [new GlassCorrespondenceFeed.Item(@"C:\ws\b.md", "adr")],
            StatusLine: "crs · test",
            FeatureLine: "feat · demo",
            AdrLine: "adr · 1",
            DocsCoverageLine: "",
            LayersBadge: "");

        var chips = GlassCorrespondenceFeed.BuildInstrument(snap, @"C:\ws\Main.cs");
        Assert.Equal(6, chips.Count);
        Assert.Equal("CRS", chips[0].Label);
        Assert.Equal("LIVE", chips[0].Value);
        Assert.Equal("REV", chips[4].Label);
        Assert.Equal("1", chips[4].Value);
        Assert.Equal("FWD", chips[5].Label);
        Assert.Equal("1", chips[5].Value);
    }
}

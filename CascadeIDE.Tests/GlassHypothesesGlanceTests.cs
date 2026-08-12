#nullable enable
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassHypothesesGlanceTests
{
    [Fact]
    public void Format_missing_marks_MISSING()
    {
        var body = GlassHypothesesGlance.Format(
            new GlassHypothesesGlance.HypothesesFsStatus(
                FilePath: @"D:\ws\.cascade-ide\debug-hypotheses.json",
                FileExists: false,
                Total: 0,
                Open: 0,
                Rejected: 0,
                Confirmed: 0,
                ModifiedUtc: null),
            workspaceRoot: @"D:\ws");

        Assert.Contains("Hypotheses glance · MISSING", body);
        Assert.Contains("file · .cascade-ide/debug-hypotheses.json", body);
        Assert.Contains("■ Glass JSON status", body);
        Assert.Contains("□ Avalonia Hypotheses", body);
    }

    [Fact]
    public void Format_ready_includes_counts()
    {
        var mtime = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var body = GlassHypothesesGlance.Format(
            new GlassHypothesesGlance.HypothesesFsStatus(
                FilePath: @"D:\ws\.cascade-ide\debug-hypotheses.json",
                FileExists: true,
                Total: 3,
                Open: 1,
                Rejected: 1,
                Confirmed: 1,
                ModifiedUtc: mtime),
            workspaceRoot: @"D:\ws");

        Assert.Contains("Hypotheses glance · READY", body);
        Assert.Contains("count · 3 · open=1 rejected=1 confirmed=1", body);
    }

    [Fact]
    public void CountFromJson_parses_status_buckets()
    {
        const string json = """
            { "version": 1, "hypotheses": [
              { "id": "a", "text": "one", "status": "open" },
              { "id": "b", "text": "two", "status": "rejected" },
              { "id": "c", "text": "three", "status": "confirmed" }
            ]}
            """;

        var (total, open, rejected, confirmed) = GlassHypothesesGlance.CountFromJson(json);
        Assert.Equal(3, total);
        Assert.Equal(1, open);
        Assert.Equal(1, rejected);
        Assert.Equal(1, confirmed);
    }

    [Fact]
    public void TryProbe_null_root_still_returns_instrument_status()
    {
        var probe = GlassHypothesesGlance.TryProbe(null);
        Assert.NotNull(probe);
        var chips = GlassGlanceCards.BuildHypotheses(probe.Value);
        Assert.Contains(chips, c => c.Label == "LEVEL");
    }
}

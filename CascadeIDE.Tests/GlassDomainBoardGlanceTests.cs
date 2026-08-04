#nullable enable
using Xunit;
using CascadeIDE.SoftOrgan;

namespace CascadeIDE.Tests;

public sealed class GlassDomainBoardGlanceTests
{
    [Fact]
    public void TryProbe_reads_domain_md_cards()
    {
        var root = Path.Combine(Path.GetTempPath(), "domain-board-" + Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, ".cdp", "domain");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "ignite.md"), """
            # Domain card: AutoIgnition
            - id: `ignite`

            ## last_ship
            - 2026-07-31: Autonomous Continuity Contract stamped
            """);
        File.WriteAllText(Path.Combine(dir, "softorgan-human-viz.md"), """
            # Domain: softorgan-human-viz

            ## last_ship
            - 2026-08-04 · Arch board
            """);

        try
        {
            var snap = GlassDomainBoardGlance.TryProbe(root);
            Assert.NotNull(snap);
            Assert.Equal(2, snap!.CardCount);
            Assert.Contains(snap.Cards, c => c.Id == "ignite");
            Assert.Contains(snap.Cards, c => c.Id == "softorgan-human-viz");

            var chips = GlassDomainBoardGlance.BuildInstrument(snap);
            Assert.Equal(new GlassGlanceChip("DOM", "LIVE", "ok"), chips[0]);
            Assert.Equal(new GlassGlanceChip("CARDS", "2", "ok"), chips[1]);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}

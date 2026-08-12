#nullable enable
using Xunit;
using CascadeIDE.SoftInstrument;

namespace CascadeIDE.Tests;

public sealed class GlassArchBoardGlanceTests
{
    [Fact]
    public void TryProbe_reads_as_built_roles()
    {
        var root = Path.Combine(Path.GetTempPath(), "arch-glance-" + Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, ".cdp", "arch-board");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "AS_BUILT.json"), """
            {
              "schema": "arch_board/v0",
              "mode": "as_built",
              "profile": "cide",
              "focus_role_id": "databus-core",
              "roles": [
                {
                  "id": "transport-ingest",
                  "role": "transport",
                  "status": "promoted",
                  "elected_candidate_id": "c1",
                  "candidates": [
                    { "id": "c1", "label": "BuildLogIngestion", "status": "elected" }
                  ]
                },
                {
                  "id": "dal-gap",
                  "role": "dal",
                  "status": "open",
                  "candidates": []
                }
              ],
              "edges": [ { "from": "a", "to": "b" } ]
            }
            """);

        try
        {
            var snap = GlassArchBoardGlance.TryProbe(root);
            Assert.NotNull(snap);
            Assert.Equal("as_built", snap!.Mode);
            Assert.Equal("cide", snap.Profile);
            Assert.Equal(2, snap.RoleCount);
            Assert.Equal(1, snap.OpenCount);
            Assert.Equal(1, snap.PromotedCount);
            Assert.Equal(1, snap.EdgeCount);
            Assert.Contains("BuildLogIngestion", snap.Roles[0].Display);
            Assert.Contains("○ dal", snap.Roles[1].Display);

            var chips = GlassArchBoardGlance.BuildInstrument(snap);
            Assert.Equal(6, chips.Count);
            Assert.Equal(new GlassGlanceChip("ARCH", "LIVE", "ok"), chips[0]);
            Assert.Equal(new GlassGlanceChip("MODE", "as_built", "ok"), chips[1]);
            Assert.Equal(new GlassGlanceChip("OPEN", "1", "warn"), chips[4]);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}

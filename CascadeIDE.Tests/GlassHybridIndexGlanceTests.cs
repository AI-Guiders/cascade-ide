#nullable enable
using Xunit;
using CascadeIDE.SoftOrgan;

namespace CascadeIDE.Tests;

public sealed class GlassHybridIndexGlanceTests
{
    [Fact]
    public void Format_missing_marks_MISSING_and_Glass_host()
    {
        var body = GlassHybridIndexGlance.Format(
            new GlassHybridIndexGlance.IndexFsStatus(
                DatabasePath: @"D:\ws\.hybrid-codebase-index\codebase-index-v2.sqlite",
                DatabaseExists: false,
                ByteLength: null,
                ModifiedUtc: null),
            workspaceRoot: @"D:\ws");

        Assert.Contains("HybridIndex glance · MISSING", body);
        Assert.Contains("db · .hybrid-codebase-index/codebase-index-v2.sqlite", body);
        Assert.Contains("■ Glass FS status", body);
        Assert.Contains("□ Avalonia HCI SSOT", body);
        Assert.Contains("index not built", body);
    }

    [Fact]
    public void Format_ready_includes_size_and_mtime()
    {
        var mtime = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var body = GlassHybridIndexGlance.Format(
            new GlassHybridIndexGlance.IndexFsStatus(
                DatabasePath: @"D:\ws\.hybrid-codebase-index\codebase-index-v2.sqlite",
                DatabaseExists: true,
                ByteLength: 2048,
                ModifiedUtc: mtime),
            workspaceRoot: @"D:\ws");

        Assert.Contains("HybridIndex glance · READY", body);
        Assert.Contains("size · 2 KB", body);
        Assert.Contains("2026-08-01 12:00:00Z", body);
    }

    [Fact]
    public void TryResolveDatabasePath_joins_default_dir()
    {
        var path = GlassHybridIndexGlance.TryResolveDatabasePath(@"D:\Experiments\ws");
        Assert.NotNull(path);
        Assert.EndsWith(
            Path.Combine(".hybrid-codebase-index", "codebase-index-v2.sqlite"),
            path!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildInstrument_projects_hci_docs_fresh()
    {
        var chips = GlassHybridIndexGlance.BuildInstrument(new GlassHybridIndexGlance.LiveInstrumentStatus(
            DatabaseExists: true,
            DocumentCount: 120,
            DocumentCountMayBeStale: false,
            IndexedAtIso: DateTimeOffset.UtcNow.AddMinutes(-5).ToString("o"),
            ReindexState: "idle",
            LastReindexError: null,
            DatabasePath: @"D:\ws\.hybrid-codebase-index\codebase-index-v2.sqlite",
            WorkspaceRoot: @"D:\ws",
            ByteLength: 4096,
            ModifiedUtc: DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.Equal(new GlassGlanceChip("HCI", "READY", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("DOCS", "120", "ok"), chips);
        Assert.Contains(chips, c => c.Label == "FRESH" && c.Tone == "ok");
        Assert.Contains(new GlassGlanceChip("ERR", "—", "idle"), chips);
    }

    [Fact]
    public void BuildScopeMap_hub_plus_folders()
    {
        var root = Path.Combine(Path.GetTempPath(), "hci-map-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Features"));
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        try
        {
            var graph = GlassHybridIndexGlance.BuildScopeMap(root, indexReady: true);
            Assert.NotNull(graph.FocusPath);
            Assert.Contains(graph.Nodes, n => n.Hop == 0 && n.Kind == "index-root");
            Assert.Contains(graph.Nodes, n => n.Hop == 1 && Path.GetFileName(n.FilePath) == "Features");
            Assert.DoesNotContain(graph.Nodes, n => Path.GetFileName(n.FilePath) == "bin");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TrySearch_empty_query_returns_error()
    {
        var r = GlassHybridIndexStatusProbe.TrySearch(@"D:\ws", "   ");
        Assert.Equal("empty query", r.Error);
        Assert.Empty(r.Hits);
    }

    [Fact]
    public void TryReindex_missing_workspace_returns_fail()
    {
        var r = GlassHybridIndexStatusProbe.TryReindex(null);
        Assert.False(r.Ok);
        Assert.Equal("workspace root unavailable", r.Message);
    }

}

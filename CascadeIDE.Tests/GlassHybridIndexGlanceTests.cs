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
}

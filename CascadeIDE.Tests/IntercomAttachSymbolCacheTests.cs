using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAttachSymbolCacheTests
{
    private const string SampleSource =
        """
        namespace Sample;

        public class Worker
        {
            public void Run() { }
        }
        """;

    [Fact]
    public void ScopeKey_IsStableForSameWorkspaceAndSolution()
    {
        var a = IntercomAttachResolveScopeKey.From(@"D:\ws", @"D:\ws\a.sln");
        var b = IntercomAttachResolveScopeKey.From(@"D:\ws", @"D:\ws\a.sln");
        Assert.Equal(a, b);
        Assert.NotEqual(a, IntercomAttachResolveScopeKey.From(@"D:\ws", @"D:\ws\b.sln"));
    }

    [Fact]
    public void WorkspaceCache_ReusesParseAcrossCalls()
    {
        var dir = createWorkspace();
        var path = Path.Combine(dir, "Worker.cs");
        File.WriteAllText(path, SampleSource);
        var scope = IntercomAttachResolveScopeKey.From(dir, null);
        var cache = IntercomAttachResolveCacheContext.From(dir, null, "Worker.cs");

        Assert.True(
            AttachmentAnchorRoslynResolver.TryGetOrCreateEntry(null, path, cache, out var first, out var d1),
            d1);
        Assert.True(
            IntercomAttachmentRoslynWorkspaceCache.TryGet(scope, path, out var cached));
        Assert.Same(first.Tree, cached.Tree);

        File.WriteAllText(path, SampleSource + "\n// touch");
        Assert.False(IntercomAttachmentRoslynWorkspaceCache.TryGet(scope, path, out _));
    }

    [Fact]
    public void SymbolSidecar_ResolvesMemberAfterIndex()
    {
        var dir = createWorkspace();
        var path = Path.Combine(dir, "Worker.cs");
        File.WriteAllText(path, SampleSource);
        var cache = IntercomAttachResolveCacheContext.From(dir, null, "Worker.cs");

        IntercomSymbolLineIndexBuilder.IndexFile(cache, path, "Worker.cs");

        Assert.True(
            IntercomSymbolLineIndex.TryResolveMemberLines(cache, path, "Run", out var lines, out var detail),
            detail);
        Assert.True(lines.Start.Value >= 5);
    }

    [Fact]
    public void TryResolveLineRange_UsesSymbolSidecarBeforeParse()
    {
        var dir = createWorkspace();
        var path = Path.Combine(dir, "Worker.cs");
        File.WriteAllText(path, SampleSource);
        var cache = IntercomAttachResolveCacheContext.From(dir, null, "Worker.cs");
        IntercomSymbolLineIndexBuilder.IndexFile(cache, path, "Worker.cs");

        Assert.True(
            AttachmentAnchorRoslynResolver.TryResolveLineRange(
                null,
                path,
                "Run",
                null,
                cache,
                out var lines,
                out var detail),
            detail);
        Assert.Contains("symbol_cache", detail, StringComparison.Ordinal);
    }

    private static string createWorkspace()
    {
        var dir = Path.Combine(
            Path.GetTempPath(),
            nameof(IntercomAttachSymbolCacheTests) + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

#nullable enable

using CascadeIDE.Features.HybridIndex.Application;

namespace CascadeIDE.Services.Intercom;

/// <summary>Фоновая синхронизация symbol sidecar с HCI reindex (ADR 0135).</summary>
public static class IntercomSymbolLineIndexCoordinator
{
    private static int _rebuildGeneration;

    public static void ScheduleRebuildAfterHybridIndex(
        string workspaceRoot,
        string? solutionPath,
        string indexDirectoryRelative)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        var gen = Interlocked.Increment(ref _rebuildGeneration);
        var root = workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var sln = string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim();
        var indexDir = HybridIndexIndexDirectoryRelative.ResolveOrDefault(indexDirectoryRelative);
        var cache = new IntercomAttachResolveCacheContext(root, sln, null, indexDir);

        _ = Task.Run(() => rebuildAsync(cache, gen, CancellationToken.None));
    }

    private static async Task rebuildAsync(
        IntercomAttachResolveCacheContext cache,
        int generation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cache.WorkspaceRoot))
            return;

        var files = IntercomSymbolLineIndexScanner.EnumerateCsFiles(
            cache.WorkspaceRoot,
            cache.IndexDirectoryRelative).ToList();

        foreach (var absolute in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != Volatile.Read(ref _rebuildGeneration))
                return;

            var rel = Path.GetRelativePath(cache.WorkspaceRoot!, absolute).Replace('\\', '/');
            var perFile = cache with { RelativePath = rel };
            try
            {
                IntercomSymbolLineIndexBuilder.IndexFile(perFile, absolute, rel);
            }
            catch
            {
                // best-effort per file
            }

            await Task.Yield();
        }
    }
}

#nullable enable

using System.Collections.Concurrent;

namespace CascadeIDE.Services.Intercom;

/// <summary>In-memory parse/model cache per solution scope (ADR 0135 L1).</summary>
public static class IntercomAttachmentRoslynWorkspaceCache
{
    private sealed class ScopeCache
    {
        internal readonly ConcurrentDictionary<string, CachedFile> Files = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record CachedFile(long LastWriteUtcTicks, IntercomAttachmentRoslynResolveSession.FileEntry Entry);

    private static readonly ConcurrentDictionary<string, ScopeCache> Scopes = new(StringComparer.Ordinal);

    public static bool TryGet(
        string scopeKey,
        string absoluteFilePath,
        out IntercomAttachmentRoslynResolveSession.FileEntry entry)
    {
        entry = null!;
        if (string.IsNullOrWhiteSpace(scopeKey) || string.IsNullOrWhiteSpace(absoluteFilePath))
            return false;

        if (!Scopes.TryGetValue(scopeKey, out var scope))
            return false;

        if (!scope.Files.TryGetValue(absoluteFilePath, out var cached))
            return false;

        if (!File.Exists(absoluteFilePath))
        {
            scope.Files.TryRemove(absoluteFilePath, out _);
            return false;
        }

        long ticks;
        try
        {
            ticks = File.GetLastWriteTimeUtc(absoluteFilePath).Ticks;
        }
        catch
        {
            return false;
        }

        if (cached.LastWriteUtcTicks != ticks)
        {
            scope.Files.TryRemove(absoluteFilePath, out _);
            return false;
        }

        entry = cached.Entry;
        return true;
    }

    public static void Store(
        string scopeKey,
        string absoluteFilePath,
        IntercomAttachmentRoslynResolveSession.FileEntry entry)
    {
        if (string.IsNullOrWhiteSpace(scopeKey) || string.IsNullOrWhiteSpace(absoluteFilePath))
            return;

        long ticks;
        try
        {
            ticks = File.GetLastWriteTimeUtc(absoluteFilePath).Ticks;
        }
        catch
        {
            return;
        }

        var scope = Scopes.GetOrAdd(scopeKey, static _ => new ScopeCache());
        scope.Files[absoluteFilePath] = new CachedFile(ticks, entry);
    }

    public static void InvalidateFile(string scopeKey, string absoluteFilePath)
    {
        if (string.IsNullOrWhiteSpace(scopeKey) || string.IsNullOrWhiteSpace(absoluteFilePath))
            return;

        if (Scopes.TryGetValue(scopeKey, out var scope))
            scope.Files.TryRemove(absoluteFilePath, out _);
    }

    public static void ClearScope(string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(scopeKey))
            return;
        Scopes.TryRemove(scopeKey, out _);
    }
}

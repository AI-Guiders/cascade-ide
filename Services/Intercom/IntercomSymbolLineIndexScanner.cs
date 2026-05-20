#nullable enable

using CascadeIDE.Features.HybridIndex.Application;

namespace CascadeIDE.Services.Intercom;

/// <summary>Обход .cs под workspace для symbol sidecar (ADR 0135).</summary>
internal static class IntercomSymbolLineIndexScanner
{
    private static readonly string[] SkipDirNames =
    [
        ".git",
        "bin",
        "obj",
        "node_modules",
        ".hybrid-codebase-index",
    ];

    public static IEnumerable<string> EnumerateCsFiles(string workspaceRoot, string indexDirectoryRelative)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            yield break;

        var indexMarker = HybridIndexIndexDirectoryRelative.ResolveOrDefault(indexDirectoryRelative)
            .Replace('\\', '/');

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(workspaceRoot, "*", SearchOption.AllDirectories);
        }
        catch
        {
            yield break;
        }

        var skipRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            if (SkipDirNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                skipRoots.Add(dir);
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(workspaceRoot, "*.cs", SearchOption.AllDirectories);
        }
        catch
        {
            yield break;
        }

        foreach (var file in files)
        {
            if (isUnderSkippedRoot(file, skipRoots))
                continue;

            var rel = Path.GetRelativePath(workspaceRoot, file).Replace('\\', '/');
            if (rel.Contains(indexMarker, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return file;
        }
    }

    private static bool isUnderSkippedRoot(string filePath, HashSet<string> skipRoots)
    {
        foreach (var root in skipRoots)
        {
            if (filePath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(filePath, root, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

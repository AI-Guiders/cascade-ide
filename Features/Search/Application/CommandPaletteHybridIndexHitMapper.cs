#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Features.Search.DataAcquisition;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.Features.Search.Application;

internal static class CommandPaletteHybridIndexHitMapper
{
    internal static RipgrepWorkspaceMatch ToRipgrepCompatibleMatch(IndexHit hit, string workspaceRootNorm)
    {
        var root = CanonicalFilePath.Normalize(workspaceRootNorm.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var relative = hit.Path.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.IsPathRooted(relative)
            ? CanonicalFilePath.Normalize(relative)
            : CanonicalFilePath.Normalize(Path.Combine(root, relative));

        var lineNum = Math.Max(hit.LineStart, 1);
        var lineText = (hit.Snippet ?? "").TrimEnd('\r', '\n');
        return new RipgrepWorkspaceMatch(path, lineNum, lineText);
    }

    internal static IReadOnlyList<RipgrepWorkspaceMatch> MapHits(IReadOnlyList<IndexHit> hits, string workspaceRootNorm)
    {
        if (hits.Count == 0)
            return Array.Empty<RipgrepWorkspaceMatch>();

        var list = new RipgrepWorkspaceMatch[hits.Count];
        for (var i = 0; i < hits.Count; i++)
            list[i] = ToRipgrepCompatibleMatch(hits[i], workspaceRootNorm);
        return list;
    }
}

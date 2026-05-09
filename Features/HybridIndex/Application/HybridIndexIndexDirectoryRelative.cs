#nullable enable
using System.IO;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>
/// Policy: normalize the configured relative index directory for HCI.
/// Keeps "where index lives under workspace root" decisions out of UI and orchestration code.
/// </summary>
[ComputingUnit("policy-hybrid-index-path")]
public static class HybridIndexIndexDirectoryRelative
{
    public static string ResolveOrDefault(string? configuredRelativeDir)
    {
        var dir = (configuredRelativeDir ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dir))
            return ".hybrid-codebase-index";

        // Security / portability: never allow rooted paths to escape the workspace root.
        if (Path.IsPathRooted(dir))
            return ".hybrid-codebase-index";

        dir = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrWhiteSpace(dir) ? ".hybrid-codebase-index" : dir;
    }
}


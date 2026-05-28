using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

internal static class WorkspaceFeatureResolver
{
    public static UiWorkspaceFeatureToml? ResolveFeatureFromWorkspaceToml(
        UiWorkspaceToml? workspaceToml,
        string repositoryRootDirectory,
        string absoluteFilePath)
    {
        var features = workspaceToml?.Workspace?.Features?.Feature;
        if (features is not { Count: > 0 })
            return null;

        var rel = TryComputeRepoRelativePath(repositoryRootDirectory, absoluteFilePath);
        if (rel is null)
            return null;

        var normalizedRel = NormalizePath(rel);

        UiWorkspaceFeatureToml? best = null;
        var bestLen = -1;

        foreach (var f in features)
        {
            if (f.Paths is not { Count: > 0 })
                continue;

            foreach (var raw in f.Paths)
            {
                var p = NormalizePath(raw);
                if (p.Length == 0)
                    continue;

                if (!normalizedRel.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (p.Length > bestLen)
                {
                    best = f;
                    bestLen = p.Length;
                }
            }
        }

        return best;
    }

    public static string BuildFeatureLine(UiWorkspaceFeatureToml? feature)
    {
        if (feature is null)
            return "";

        var title = (feature.Title ?? "").Trim();
        var id = (feature.Id ?? "").Trim();
        if (title.Length > 0 && id.Length > 0)
            return $"Feature: {title} ({id})";
        if (title.Length > 0)
            return $"Feature: {title}";
        if (id.Length > 0)
            return $"Feature: {id}";
        return "";
    }

    private static string NormalizePath(string raw) =>
        (raw ?? "")
            .Trim()
            .Replace('\\', '/');

    private static string? TryComputeRepoRelativePath(string repositoryRootDirectory, string absoluteFilePath)
    {
        try
        {
            var root = CanonicalFilePath.Normalize(repositoryRootDirectory.Trim());
            var abs = CanonicalFilePath.Normalize(absoluteFilePath.Trim());
            if (!abs.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;

            var rel = abs[root.Length..].TrimStart('\\', '/');
            return rel;
        }
        catch
        {
            return null;
        }
    }
}


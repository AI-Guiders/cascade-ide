using System.Text.RegularExpressions;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

internal static partial class WorkspaceAdrMapResolver
{
    public const string AutoIncludeNone = "none";
    public const string AutoIncludeLinked = "linked";

    public static IReadOnlyList<string> ResolveAdrDocPathsFromWorkspaceToml(
        UiWorkspaceToml? workspaceToml,
        string repositoryRootDirectory,
        string absoluteFilePath)
    {
        if (workspaceToml?.Workspace?.Adr?.Map is not { Count: > 0 } map)
            return [];

        var rel = TryComputeRepoRelativePath(repositoryRootDirectory, absoluteFilePath);
        if (rel is null)
            return [];

        var normalizedRel = NormalizeTomlPath(rel);

        string? bestKey = null;
        var bestLen = -1;
        foreach (var rawKey in map.Keys)
        {
            var k = NormalizeTomlPath(rawKey);
            if (k == "*")
            {
                if (bestKey is null)
                {
                    bestKey = rawKey;
                    bestLen = 0;
                }
                continue;
            }

            if (!normalizedRel.StartsWith(k, StringComparison.OrdinalIgnoreCase))
                continue;

            if (k.Length > bestLen)
            {
                bestKey = rawKey;
                bestLen = k.Length;
            }
        }

        if (bestKey is null || !map.TryGetValue(bestKey, out var v))
            return [];

        return ExtractStringList(v)
            .Select(x => NormalizeDocPath(x))
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string BuildAdrIndicatorLine(IReadOnlyList<string> adrDocPaths)
    {
        if (adrDocPaths.Count == 0)
            return "";

        var ids = new List<string>(adrDocPaths.Count);
        foreach (var p in adrDocPaths)
        {
            var id = TryExtractAdrId(p);
            ids.Add(id ?? p);
        }

        if (ids.Count == 1)
            return $"ADR: {ids[0]}";

        // Keep it compact in PFD/MFD: show first and count.
        return $"ADR: {ids[0]} (+{ids.Count - 1})";
    }

    public static string NormalizeAutoInclude(string? raw)
    {
        var v = (raw ?? "").Trim().ToLowerInvariant();
        return v switch
        {
            AutoIncludeLinked => AutoIncludeLinked,
            _ => AutoIncludeNone
        };
    }

    public static string? TryResolveAbsoluteDocPath(string repositoryRootDirectory, string repoRelativeDocPath)
    {
        var rel = (repoRelativeDocPath ?? "").Trim();
        if (rel.Length == 0)
            return null;

        // Treat "docs/..." as repo-relative (not absolute on Windows).
        rel = rel.Replace('\\', '/');
        if (Path.IsPathRooted(rel))
            return rel;

        try
        {
            var abs = Path.Combine(repositoryRootDirectory.Trim(), rel.Replace('/', Path.DirectorySeparatorChar));
            return CanonicalFilePath.Normalize(abs);
        }
        catch
        {
            return null;
        }
    }

    public static string GuessAdrPreviewTitle(string repoRelativeDocPath)
    {
        var id = TryExtractAdrId(repoRelativeDocPath);
        return id is null ? repoRelativeDocPath : $"{id}";
    }

    public static IReadOnlyList<string> ExtractLinkedAdrDocPathsFromMarkdown(
        string markdown,
        string currentDocRepoRelativePath)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var current = NormalizeDocPath(currentDocRepoRelativePath);
        var list = new List<string>();

        foreach (Match m in MarkdownLinkRegex().Matches(markdown))
        {
            var raw = m.Groups["target"].Value;
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            raw = raw.Trim();
            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                continue;

            var resolved = ResolveMarkdownLinkTargetToRepoRelative(raw, current);
            if (resolved is null)
                continue;

            if (!resolved.StartsWith("docs/adr/", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(resolved, current, StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(resolved);
        }

        return list
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ResolveMarkdownLinkTargetToRepoRelative(string target, string currentDocRepoRelativePath)
    {
        var t = (target ?? "").Trim().Replace('\\', '/');
        if (t.Length == 0)
            return null;

        // Strip anchors/fragments.
        var hash = t.IndexOf('#');
        if (hash >= 0)
            t = t[..hash];

        if (t.Length == 0)
            return null;

        // Absolute-ish repo path.
        if (t.StartsWith("docs/adr/", StringComparison.OrdinalIgnoreCase))
            return NormalizeDocPath(t);

        // Relative path: resolve against current doc directory.
        if (t.StartsWith("./", StringComparison.Ordinal) || t.StartsWith("../", StringComparison.Ordinal))
        {
            var cur = NormalizeDocPath(currentDocRepoRelativePath);
            var lastSlash = cur.LastIndexOf('/');
            var baseDir = lastSlash >= 0 ? cur[..(lastSlash + 1)] : "";
            var combined = baseDir + t;

            // Normalize /./ and /../ segments.
            var parts = new List<string>();
            foreach (var p in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (p == ".")
                    continue;
                if (p == "..")
                {
                    if (parts.Count > 0)
                        parts.RemoveAt(parts.Count - 1);
                    continue;
                }
                parts.Add(p);
            }

            return parts.Count == 0 ? null : string.Join('/', parts);
        }

        return null;
    }

    private static string NormalizeDocPath(string raw)
    {
        var s = (raw ?? "").Trim();
        if (s.Length == 0)
            return "";
        return s.Replace('\\', '/');
    }

    private static string NormalizeTomlPath(string rawKey)
    {
        var s = (rawKey ?? "").Trim().Replace('\\', '/');
        return s;
    }

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

    private static IReadOnlyList<string> ExtractStringList(object? v)
    {
        if (v is null)
            return [];

        if (v is string s)
            return [s];

        if (v is IEnumerable<object> objs)
        {
            var list = new List<string>();
            foreach (var o in objs)
            {
                if (o is string os && os.Trim().Length > 0)
                    list.Add(os);
            }
            return list;
        }

        return [];
    }

    private static string? TryExtractAdrId(string docPath)
    {
        var p = (docPath ?? "").Replace('\\', '/');
        var m = AdrIdFromPathRegex().Match(p);
        if (!m.Success)
            return null;
        return $"ADR {m.Groups["id"].Value}";
    }

    [GeneratedRegex(@"(?:^|/)docs/adr/(?<id>\d{4})-", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdrIdFromPathRegex();

    // Markdown links: [text](target)
    [GeneratedRegex(@"\[[^\]]*\]\((?<target>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLinkRegex();
}


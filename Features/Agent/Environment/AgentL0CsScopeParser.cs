namespace CascadeIDE.Features.Agent.Environment;

internal static class AgentL0CsScopeParser
{
    public const string OpenTabsOnly = "open_tabs";

    /// <summary>Открытые вкладки + отслеживаемые <c>.cs</c> из <c>git diff</c> (рабочее дерево + индекс).</summary>
    public const string OpenTabsAndGitDirtyCs = "open_tabs_and_git_dirty_cs";

    public static bool IncludesGitDirtyWorktreeCs(string? scope) =>
        string.Equals(scope?.Trim(), OpenTabsAndGitDirtyCs, StringComparison.OrdinalIgnoreCase);

    /// <summary>Объединяет несколько блоков stdout <c>git diff --name-only</c>; только непустые <c>.cs</c>, сохраняя порядок первого появления.</summary>
    internal static IReadOnlyList<string> MergeGitNameOnlyOutputs(params string?[] blocks)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block))
                continue;
            foreach (var line in block.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                if (!trimmed.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!seen.Add(trimmed))
                    continue;
                ordered.Add(trimmed);
            }
        }

        return ordered;
    }

    /// <summary>Относительный путь из git → абсолютный только если попадает в workspace.</summary>
    internal static bool TryResolveWorkspaceCs(string workspaceRootFull, string relativeFromGit, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? fullPath)
    {
        fullPath = null;
        if (relativeFromGit.Length == 0)
            return false;

        try
        {
            var collapsed = relativeFromGit.Replace('/', Path.DirectorySeparatorChar);
            var root = Path.GetFullPath(workspaceRootFull).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(root, collapsed));
            if (!IsStrictSubPathOrEqualFile(root, candidate))
                return false;
            if (!File.Exists(candidate))
                return false;

            fullPath = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsStrictSubPathOrEqualFile(string rootFull, string candidateFull)
    {
        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (string.Equals(rootFull, candidateFull, cmp))
            return false;

        var prefix = rootFull + Path.DirectorySeparatorChar;
        if (!candidateFull.StartsWith(prefix, cmp))
            return false;

        try
        {
            return Path.GetRelativePath(rootFull, candidateFull)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .All(s => !string.Equals(s, "..", cmp));
        }
        catch
        {
            return false;
        }
    }
}

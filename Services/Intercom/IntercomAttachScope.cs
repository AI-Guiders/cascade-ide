#nullable enable

namespace CascadeIDE.Services.Intercom;

/// <summary>Workspace + solution для resolve/reveal якорей без открытого решения в UI (meta сессии).</summary>
public static class IntercomAttachScope
{
    /// <summary>Абсолютный путь к .sln/.slnx/.csproj: live UI, иначе из meta (относительно workspace).</summary>
    public static string? ResolveSolutionPath(
        string? workspaceRoot,
        string? liveSolutionPath,
        string? sessionSolutionPathRelative)
    {
        var live = liveSolutionPath?.Trim();
        if (!string.IsNullOrEmpty(live))
            return live;

        if (string.IsNullOrWhiteSpace(sessionSolutionPathRelative)
            || string.IsNullOrWhiteSpace(workspaceRoot))
        {
            return null;
        }

        return AttachmentAnchorPaths.TryResolveAbsolute(
            sessionSolutionPathRelative.Trim(),
            workspaceRoot,
            out var absolute,
            out _)
            ? absolute
            : null;
    }

    /// <summary>Относительный путь решения для <c>ChatSessionMetadata.SolutionPath</c>.</summary>
    public static string? ToSessionRelativeSolutionPath(string? liveSolutionPath, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(liveSolutionPath))
            return null;

        return AttachmentAnchorPaths.ToWorkspaceRelative(liveSolutionPath, workspaceRoot)
            ?? liveSolutionPath.Trim();
    }

    /// <summary>Best-effort: один .slnx/.sln в корне workspace (для meta без открытого решения).</summary>
    public static string? TryDiscoverSolutionPathRelative(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return null;

        var root = workspaceRoot.Trim();
        foreach (var pattern in new[] { "*.slnx", "*.sln", "*.slnf" })
        {
            var matches = Directory.EnumerateFiles(root, pattern, SearchOption.TopDirectoryOnly)
                .Take(2)
                .ToList();
            if (matches.Count != 1)
                continue;

            return AttachmentAnchorPaths.ToWorkspaceRelative(matches[0], root);
        }

        return null;
    }
}

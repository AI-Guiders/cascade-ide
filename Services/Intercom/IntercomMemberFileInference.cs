#nullable enable

using CascadeIDE.Features.HybridIndex.Application;

namespace CascadeIDE.Services.Intercom;

/// <summary>Infer workspace-relative <c>F:</c> from <c>M:</c> via symbol sidecar (ADR 0128, 0135).</summary>
public static class IntercomMemberFileInference
{
    private const int MaxAmbiguousPathsListed = 5;

    public static bool TryResolveRelativeFile(
        string? explicitRelativeFile,
        string? memberKey,
        string? activeFilePath,
        string? workspaceRoot,
        string? solutionPath,
        string? indexDirectoryRelative,
        out string relativeFile,
        out string error)
    {
        relativeFile = "";
        error = "";

        var explicitPath = normalizeRelativePath(explicitRelativeFile);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            relativeFile = explicitPath;
            return true;
        }

        if (string.IsNullOrWhiteSpace(memberKey))
        {
            var activeOnly = normalizeRelativePath(
                AttachmentAnchorPaths.ToWorkspaceRelative(activeFilePath, workspaceRoot) ?? activeFilePath);
            if (string.IsNullOrWhiteSpace(activeOnly))
            {
                error = "Не задан файл в ссылке и нет активного файла в редакторе.";
                return false;
            }

            relativeFile = activeOnly;
            return true;
        }

        var indexDir = HybridIndexIndexDirectoryRelative.ResolveOrDefault(indexDirectoryRelative);
        var activeRel = normalizeRelativePath(
            AttachmentAnchorPaths.ToWorkspaceRelative(activeFilePath, workspaceRoot) ?? activeFilePath);

        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            if (!string.IsNullOrWhiteSpace(activeRel))
            {
                relativeFile = activeRel;
                return true;
            }

            error = "Нет workspace для поиска члена по solution.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(activeRel)
            && memberExistsInFile(workspaceRoot, solutionPath, indexDir, activeRel, memberKey))
        {
            relativeFile = activeRel;
            return true;
        }

        var cache = IntercomAttachResolveCacheContext.From(workspaceRoot, solutionPath, null, indexDir);
        if (!IntercomSymbolLineIndex.TryFindRelativePathsForMember(cache, memberKey, out var paths))
            paths = [];

        if (paths.Count == 1)
        {
            relativeFile = paths[0];
            return true;
        }

        if (paths.Count > 1)
        {
            if (!string.IsNullOrWhiteSpace(activeRel)
                && paths.Any(p => string.Equals(p, activeRel, StringComparison.OrdinalIgnoreCase)))
            {
                relativeFile = activeRel;
                return true;
            }

            var listed = string.Join(", ", paths.Take(MaxAmbiguousPathsListed));
            var more = paths.Count > MaxAmbiguousPathsListed ? $" (+{paths.Count - MaxAmbiguousPathsListed})" : "";
            error =
                $"Член «{memberKey}» найден в нескольких файлах: {listed}{more}. Укажи F: или открой нужный файл.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(activeRel))
        {
            relativeFile = activeRel;
            return true;
        }

        error =
            $"Член «{memberKey}» не найден в symbol index. Открой файл с членом, укажи [F:… M:…] или дождись прогрева индекса.";
        return false;
    }

    private static bool memberExistsInFile(
        string workspaceRoot,
        string? solutionPath,
        string indexDir,
        string relativePath,
        string memberKey)
    {
        var cache = IntercomAttachResolveCacheContext.From(workspaceRoot, solutionPath, relativePath, indexDir);
        if (!AttachmentAnchorPaths.TryResolveAbsolute(relativePath, workspaceRoot, out var absolute, out _)
            || !File.Exists(absolute))
        {
            return false;
        }

        return IntercomSymbolLineIndex.TryResolveMemberLines(cache, absolute, memberKey, out _, out _);
    }

    private static string normalizeRelativePath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? "" : path.Trim().Replace('\\', '/');
}

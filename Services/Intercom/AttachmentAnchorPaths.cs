#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.Services.Intercom;

internal static class AttachmentAnchorPaths
{
    public static bool TryResolveAbsolute(string file, string? workspaceRoot, out string absolute, out string error) =>
        TryResolveAbsolute(file, workspaceRoot, hintActiveFilePath: null, out absolute, out error);

    /// <summary>
    /// Resolve workspace-relative or absolute <paramref name="file"/>.
    /// Prefers an on-disk path: workspace hit, then focus-LATEST basename match, then sibling of active file.
    /// </summary>
    public static bool TryResolveAbsolute(
        string file,
        string? workspaceRoot,
        string? hintActiveFilePath,
        out string absolute,
        out string error)
    {
        absolute = "";
        error = "";

        var trimmed = file.Trim();
        if (trimmed.Length == 0)
        {
            error = "пустой file.";
            return false;
        }

        if (Path.IsPathRooted(trimmed))
        {
            if (!CanonicalFilePath.TryNormalize(trimmed, out absolute))
            {
                error = "не удалось нормализовать абсолютный путь.";
                return false;
            }

            return true;
        }

        var fileName = Path.GetFileName(trimmed.Replace('/', Path.DirectorySeparatorChar));

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var combined = Path.Combine(workspaceRoot.Trim(), trimmed.Replace('/', Path.DirectorySeparatorChar));
            if (CanonicalFilePath.TryNormalize(combined, out var wsAbs) && File.Exists(wsAbs))
            {
                absolute = wsAbs;
                return true;
            }
        }

        if (fileName.Length > 0
            && TryMatchFocusLatchByFileName(fileName, out var latchAbs))
        {
            absolute = latchAbs;
            return true;
        }

        if (fileName.Length > 0
            && !string.IsNullOrWhiteSpace(hintActiveFilePath)
            && CanonicalFilePath.TryNormalize(hintActiveFilePath.Trim(), out var activeAbs))
        {
            var dir = Path.GetDirectoryName(activeAbs);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                var sibling = Path.Combine(dir, fileName);
                if (CanonicalFilePath.TryNormalize(sibling, out var sibAbs) && File.Exists(sibAbs))
                {
                    absolute = sibAbs;
                    return true;
                }
            }
        }

        // Legacy: allow non-existent workspace-relative (callers may create / reveal later).
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var combined = Path.Combine(workspaceRoot.Trim(), trimmed.Replace('/', Path.DirectorySeparatorChar));
            if (!CanonicalFilePath.TryNormalize(combined, out absolute))
            {
                error = "не удалось нормализовать путь относительно workspace.";
                return false;
            }

            return true;
        }

        error = "относительный file без загруженного workspace.";
        return false;
    }

    static bool TryMatchFocusLatchByFileName(string fileName, out string absolute)
    {
        absolute = "";
        try
        {
            var latchPath = CdpHabitatPaths.GetLatchPath("focus-LATEST.json");
            if (!File.Exists(latchPath))
                return false;

            using var doc = JsonDocument.Parse(File.ReadAllText(latchPath));
            if (!doc.RootElement.TryGetProperty("path", out var pathEl))
                return false;

            var focusPath = pathEl.GetString();
            if (string.IsNullOrWhiteSpace(focusPath))
                return false;

            if (!string.Equals(Path.GetFileName(focusPath), fileName, StringComparison.OrdinalIgnoreCase))
                return false;

            return CanonicalFilePath.TryNormalize(focusPath, out absolute) && File.Exists(absolute);
        }
        catch
        {
            return false;
        }
    }

    public static string? ToWorkspaceRelative(string absoluteOrRelative, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(absoluteOrRelative))
            return null;

        var trimmed = absoluteOrRelative.Trim();
        if (!Path.IsPathRooted(trimmed))
            return trimmed.Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(workspaceRoot)
            || !CanonicalFilePath.TryNormalize(workspaceRoot.Trim(), out var rootNorm)
            || !CanonicalFilePath.TryNormalize(trimmed, out var fileNorm))
        {
            return trimmed.Replace('\\', '/');
        }

        if (!fileNorm.StartsWith(rootNorm, StringComparison.OrdinalIgnoreCase))
            return trimmed.Replace('\\', '/');

        var rel = fileNorm.Length == rootNorm.Length
            ? ""
            : fileNorm[rootNorm.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return rel.Replace('\\', '/');
    }
}

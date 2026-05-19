#nullable enable

namespace CascadeIDE.Services.Intercom;

internal static class AttachmentAnchorPaths
{
    public static bool TryResolveAbsolute(string file, string? workspaceRoot, out string absolute, out string error)
    {
        absolute = "";
        error = "";

        var trimmed = file.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            if (!CanonicalFilePath.TryNormalize(trimmed, out absolute))
            {
                error = "не удалось нормализовать абсолютный путь.";
                return false;
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            error = "относительный file без загруженного workspace.";
            return false;
        }

        var combined = Path.Combine(workspaceRoot.Trim(), trimmed.Replace('/', Path.DirectorySeparatorChar));
        if (!CanonicalFilePath.TryNormalize(combined, out absolute))
        {
            error = "не удалось нормализовать путь относительно workspace.";
            return false;
        }

        return true;
    }
}

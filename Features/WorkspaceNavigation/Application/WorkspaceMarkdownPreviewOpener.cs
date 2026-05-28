#nullable enable

using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Открытие markdown-документа в MFD preview (correspondence / templates).</summary>
public static class WorkspaceMarkdownPreviewOpener
{
    public static bool TryOpenRepoDocument(
        string workspaceRoot,
        string relativeOrAbsoluteDocPath,
        Action<string, string, string?> setPreview,
        out string? error)
    {
        error = null;
        var abs = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(workspaceRoot, relativeOrAbsoluteDocPath);
        if (string.IsNullOrWhiteSpace(abs))
        {
            error = "path_not_resolved";
            return false;
        }

        if (!WorkspaceTextFileReader.TryReadAllText(abs, out var content))
        {
            error = "file_not_readable";
            return false;
        }

        var title = WorkspaceAdrMapResolver.GuessAdrPreviewTitle(relativeOrAbsoluteDocPath);
        setPreview(title, content, abs);
        return true;
    }
}

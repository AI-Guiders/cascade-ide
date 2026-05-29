#nullable enable

using CascadeIDE.Features.Workspace.DataAcquisition;

namespace CascadeIDE.Features.MagicLink;

/// <summary>Проверка, что root — допустимый workspace для Magic Link (ADR 0157 §2).</summary>
public static class CideMagicLinkWorkspaceGuard
{
    public static bool TryValidateRoot(string? workspaceRoot, out string normalizedRoot, out string? error)
    {
        normalizedRoot = "";
        error = null;

        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            error = "root не задан.";
            return false;
        }

        try
        {
            normalizedRoot = Path.GetFullPath(workspaceRoot.Trim());
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        if (!Directory.Exists(normalizedRoot))
        {
            error = "root не существует.";
            return false;
        }

        if (RepositoryWorkspaceTomlLoader.TryLoad(normalizedRoot) is not null)
            return true;

        var sln = Directory.EnumerateFiles(normalizedRoot, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault()
                ?? Directory.EnumerateFiles(normalizedRoot, "*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (sln is not null)
            return true;

        error = "root не похож на workspace (нет .cascade/workspace.toml и *.sln).";
        return false;
    }

    public static bool TryResolveUnderRoot(string workspaceRoot, string repoRelativePath, out string absolutePath, out string? error)
    {
        absolutePath = "";
        error = null;

        var rel = repoRelativePath.Replace('\\', '/').Trim();
        if (rel.Contains("..", StringComparison.Ordinal))
        {
            error = "path traversal запрещён.";
            return false;
        }

        try
        {
            absolutePath = Path.GetFullPath(Path.Combine(workspaceRoot, rel.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        if (!absolutePath.StartsWith(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(absolutePath, workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            error = "путь вне workspace root.";
            return false;
        }

        return true;
    }
}

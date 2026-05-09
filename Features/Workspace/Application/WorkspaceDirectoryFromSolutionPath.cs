using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Каталог workspace по пути к файлу решения (как для git/индекса).</summary>
public static class WorkspaceDirectoryFromSolutionPath
{
    public static string Resolve(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return "";
        var p = CanonicalFilePath.Normalize(solutionPath.Trim());
        return File.Exists(p) ? Path.GetDirectoryName(p) ?? "" : p;
    }
}

#nullable enable
namespace CascadeIDE.Services;

/// <summary>Разрешение пути к <c>.sln</c> из аргумента MCP <c>workspace_path</c> (каталог, файл решения или произвольный файл в дереве).</summary>
public static class DebugWorkspacePath
{
    /// <summary>
    /// Если путь указывает на <c>.sln/.slnx/.slnf</c> — возвращает его; если на каталог — ищет решение в нём;
    /// если на другой файл — ищет решение в каталоге файла.
    /// </summary>
    public static string? TryResolveWorkspaceToSolutionPath(string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return null;

        var full = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(full))
        {
            var ext = Path.GetExtension(full);
            if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".slnf", StringComparison.OrdinalIgnoreCase))
                return full;

            var dir = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(dir))
                return null;
            return SolutionFileLocator.TryFindSolutionInDirectory(dir);
        }

        if (Directory.Exists(full))
            return SolutionFileLocator.TryFindSolutionInDirectory(full);

        return null;
    }
}

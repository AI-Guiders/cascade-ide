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
            return TryResolveWhenEntryIsFile(full);

        if (Directory.Exists(full))
            return SolutionFileLocator.TryFindSolutionInDirectory(full);

        return null;
    }

    private static string? TryResolveWhenEntryIsFile(string fullPath)
    {
        if (IsSolutionFilePath(fullPath))
            return fullPath;

        var dir = Path.GetDirectoryName(fullPath);
        return string.IsNullOrEmpty(dir) ? null : SolutionFileLocator.TryFindSolutionInDirectory(dir);
    }

    private static bool IsSolutionFilePath(string fullPath) =>
        Path.GetExtension(fullPath) is var ext && IsSolutionFileExtension(ext);

    private static bool IsSolutionFileExtension(string extension) =>
        extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".slnf", StringComparison.OrdinalIgnoreCase);
}

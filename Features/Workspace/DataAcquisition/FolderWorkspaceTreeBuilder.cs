using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>
/// Дерево обозревателя для режима «папка как workspace» (без .sln): каталоги и файлы с отсечением по глубине/числу узлов.
/// </summary>
public static class FolderWorkspaceTreeBuilder
{
    private const int MaxDepth = 14;
    private const int MaxNodes = 8000;

    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "bin", "obj", "node_modules", ".vs", "packages", ".idea", "__pycache__",
        ".venv", "venv", "dist", "build", ".turbo", ".next"
    };

    /// <summary>
    /// Корень с <see cref="SolutionItem.FullPath"/> = нормализованный каталог; дочерние папки — через <see cref="SolutionItem.CreateFolder"/> (без пути).
    /// </summary>
    public static SolutionItem? TryBuild(string folderPath, out string? error)
    {
        error = null;
        folderPath = folderPath?.Trim() ?? "";
        if (folderPath.Length == 0)
        {
            error = "Путь пустой.";
            return null;
        }

        if (Uri.TryCreate(folderPath, UriKind.Absolute, out var uri) && uri.IsFile)
            folderPath = uri.LocalPath;

        string normalized;
        try
        {
            normalized = Path.GetFullPath(folderPath);
        }
        catch (Exception ex)
        {
            error = "Путь: " + ex.Message;
            return null;
        }

        if (!Directory.Exists(normalized))
        {
            error = "Каталог не найден: " + normalized;
            return null;
        }

        var title = Path.GetFileName(normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(title))
            title = normalized;

        var root = SolutionItem.CreateFolderWorkspaceRoot(title, normalized);
        var repoRoot = WorkspaceIgnoreMatcher.ResolveRepositoryRoot(normalized);
        var ignore = WorkspaceIgnoreMatcher.GetOrCreate(repoRoot);
        var count = 0;
        AddChildren(root, normalized, MaxDepth, ref count, ignore);
        ProjectFileTreeBuilder.SortSolutionItemChildren(root, StringComparer.OrdinalIgnoreCase);
        return root;
    }

    private static void AddChildren(SolutionItem parent, string dir, int depthRemaining, ref int nodeCount, WorkspaceIgnoreMatcher ignore)
    {
        if (depthRemaining <= 0 || nodeCount >= MaxNodes)
            return;

        List<string> dirs = [];
        List<string> files = [];
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                if (nodeCount >= MaxNodes)
                    break;
                var name = Path.GetFileName(entry);
                if (name.Length == 0)
                    continue;
                if (Directory.Exists(entry))
                {
                    if (ExcludedDirectoryNames.Contains(name))
                        continue;
                    if (ignore.IsIgnored(entry))
                        continue;
                    dirs.Add(entry);
                }
                else if (File.Exists(entry))
                {
                    if (ignore.IsIgnored(entry))
                        continue;
                    files.Add(entry);
                }
            }
        }
        catch
        {
            return;
        }

        dirs.Sort(StringComparer.OrdinalIgnoreCase);
        files.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (var d in dirs)
        {
            if (nodeCount >= MaxNodes)
                break;
            var folderNode = SolutionItem.CreateFolder(Path.GetFileName(d));
            parent.Children.Add(folderNode);
            nodeCount++;
            AddChildren(folderNode, d, depthRemaining - 1, ref nodeCount, ignore);
        }

        foreach (var f in files)
        {
            if (nodeCount >= MaxNodes)
                break;
            parent.Children.Add(SolutionItem.CreateFile(Path.GetFileName(f), f));
            nodeCount++;
        }
    }
}

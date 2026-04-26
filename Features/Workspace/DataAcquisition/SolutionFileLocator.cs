#nullable enable
namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>Поиск файла решения (.slnx / .sln / .slnf) относительно пути к исходнику.</summary>
public static class SolutionFileLocator
{
    /// <summary>
    /// Поднимается от каталога с <paramref name="sourceFilePath"/> к родителям и возвращает первый найденный
    /// файл решения в этой директории (приоритет: .slnx, затем .sln, затем .slnf; при нескольких — лексикографически первый).
    /// </summary>
    public static string? TryFindSolutionForSourceFile(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return null;
        if (!File.Exists(sourceFilePath))
            return null;

        var dir = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
        while (!string.IsNullOrEmpty(dir))
        {
            var found = TryFindSolutionInDirectory(dir);
            if (found is not null)
                return found;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    /// <summary>Ищет один файл решения в каталоге (без подъёма вверх по дереву).</summary>
    public static string? TryFindSolutionInDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        static string? PickFirst(IEnumerable<string> files)
        {
            var arr = files as string[] ?? files.ToArray();
            return arr.Length == 0 ? null : arr.OrderBy(static f => f, StringComparer.OrdinalIgnoreCase).First();
        }

        var slnx = Directory.GetFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly);
        var sln = Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly);
        var slnf = Directory.GetFiles(directory, "*.slnf", SearchOption.TopDirectoryOnly);
        return PickFirst(slnx) ?? PickFirst(sln) ?? PickFirst(slnf);
    }

    /// <summary>Нужно ли загрузить найденное решение вместо текущего контекста IDE (разные пути после нормализации).</summary>
    public static bool NeedsLoadSolutionBeforeBreakpoint(string? foundSolutionPath, string? currentWorkspaceSolutionPath)
    {
        if (string.IsNullOrWhiteSpace(foundSolutionPath) || !File.Exists(foundSolutionPath))
            return false;
        var candidate = Path.GetFullPath(foundSolutionPath);
        if (string.IsNullOrWhiteSpace(currentWorkspaceSolutionPath))
            return true;
        var current = Path.GetFullPath(currentWorkspaceSolutionPath.Trim());
        return !PathsReferToSameFile(candidate, current);
    }

    private static bool PathsReferToSameFile(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}

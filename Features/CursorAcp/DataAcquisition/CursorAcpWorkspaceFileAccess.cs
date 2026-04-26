namespace CascadeIDE.Features.CursorAcp.DataAcquisition;

/// <summary>
/// DAL: разрешение путей внутри workspace и чтение/запись текста (инструменты ACP, без обхода корня).
/// </summary>
public static class CursorAcpWorkspaceFileAccess
{
    /// <summary>Абсолютный путь, если <paramref name="path"/> внутри <paramref name="workspaceRoot"/>; иначе <c>null</c>.</summary>
    public static string? TryResolvePathUnderWorkspace(string workspaceRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        var full = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(workspaceRoot, path));
        var root = Path.GetFullPath(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;
        return full;
    }

    /// <summary>Записывает текст; при невалидном пути — no-op (как пустой ответ ACP).</summary>
    public static void WriteTextFileUnderWorkspace(string workspaceRoot, string? requestPath, string? content)
    {
        var path = TryResolvePathUnderWorkspace(workspaceRoot, requestPath);
        if (path is null)
            return;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, content ?? "");
    }

    /// <summary>Читает текст или пустую строку, если путь вне workspace или файла нет.</summary>
    public static string ReadTextFileOrEmpty(string workspaceRoot, string? requestPath)
    {
        var path = TryResolvePathUnderWorkspace(workspaceRoot, requestPath);
        if (path is null || !File.Exists(path))
            return "";
        return File.ReadAllText(path);
    }
}

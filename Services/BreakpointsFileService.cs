using DotnetDebug.Core;

namespace CascadeIDE.Services;

/// <summary>Операции IDE поверх <see cref="BreakpointsStorage"/> (тот же JSON, что у dotnet-debug-mcp).</summary>
public static class BreakpointsFileService
{
    public const string FileName = BreakpointsStorage.FileName;

    /// <summary>Относительный путь от корня workspace к собранной DLL тестовой цели (после <c>dotnet build</c>).</summary>
    public static readonly string DefaultDebugTargetRelativeDll = Path.Combine("samples", "DebugTarget", "bin", "Debug", "net10.0", "DebugTarget.dll");

    public static string GetFilePath(string workspacePath) => BreakpointsStorage.GetStorageFilePath(workspacePath);

    /// <summary>Корень workspace (каталог с .sln/.slnx или переданный каталог).</summary>
    public static string GetWorkspaceRoot(string workspacePath)
    {
        var dir = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(dir))
            dir = Path.GetDirectoryName(dir) ?? dir;
        return dir;
    }

    /// <summary>
    /// Полный путь к тестовой сборке <c>samples/DebugTarget</c>: используется как ключ в JSON для MCP/set_breakpoint
    /// и как стартовая папка в диалоге выбора цели. Запуск отладки всегда с выбранным пользователем <c>target_path</c> — эта DLL не стартует сама.
    /// </summary>
    public static string GetDefaultDebugTargetPath(string workspacePath) =>
        Path.GetFullPath(Path.Combine(GetWorkspaceRoot(workspacePath), DefaultDebugTargetRelativeDll));

    private static string? NormalizeEntryPath(string workspacePath, string? entryFile)
    {
        if (string.IsNullOrEmpty(entryFile))
            return null;
        if (Path.IsPathRooted(entryFile))
            return Path.GetFullPath(entryFile);
        var wsDir = Path.GetDirectoryName(GetFilePath(workspacePath));
        return string.IsNullOrEmpty(wsDir) ? null : Path.GetFullPath(Path.Combine(wsDir, entryFile));
    }

    /// <summary>Номера строк с брейкпоинтами из файла для указанного файла (по всем targets).</summary>
    public static IReadOnlyList<int> GetLinesForFile(string workspacePath, string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        var normalized = Path.GetFullPath(filePath);
        var model = BreakpointsStorage.Load(workspacePath);
        var lines = new HashSet<int>();
        foreach (var list in model.Targets.Values)
        {
            foreach (var entry in list)
            {
                var entryPath = NormalizeEntryPath(workspacePath, entry.File);
                if (entryPath != null && string.Equals(entryPath, normalized, StringComparison.OrdinalIgnoreCase))
                    lines.Add(entry.Line);
            }
        }
        return lines.OrderBy(static l => l).ToList();
    }

    /// <summary>Выбрать target для добавления/удаления: сначала тестовая <see cref="GetDefaultDebugTargetPath"/>, иначе первый под workspace, иначе первый в списке.</summary>
    public static string? GetPreferredTargetKey(string workspacePath)
    {
        var ws = GetWorkspaceRoot(workspacePath);
        var model = BreakpointsStorage.Load(workspacePath);
        var preferred = Path.GetFullPath(GetDefaultDebugTargetPath(workspacePath));
        if (model.Targets.ContainsKey(preferred))
            return preferred;
        var key = model.Targets.Keys.FirstOrDefault(k => k.StartsWith(ws, StringComparison.OrdinalIgnoreCase));
        return key ?? model.Targets.Keys.FirstOrDefault();
    }

    /// <summary>Записать брейкпоинт в JSON для цели по умолчанию (<see cref="GetDefaultDebugTargetPath"/>), в том числе из MCP <c>set_breakpoint</c>.</summary>
    public static void SetBreakpointForDefaultTarget(string workspacePath, string filePath, int line, string? condition = null)
    {
        if (line < 1 || string.IsNullOrEmpty(filePath))
            return;
        var path = Path.GetFullPath(filePath);
        var target = Path.GetFullPath(GetDefaultDebugTargetPath(workspacePath));
        var list = BreakpointsStorage.GetBreakpoints(workspacePath, target).ToList();
        list.RemoveAll(e =>
        {
            var ep = NormalizeEntryPath(workspacePath, e.File);
            return ep != null && string.Equals(ep, path, StringComparison.OrdinalIgnoreCase) && e.Line == line;
        });
        list.Add(new BreakpointsStorage.BreakpointEntry(path, line, condition));
        BreakpointsStorage.SetBreakpoints(workspacePath, target, list);
    }

    /// <summary>Удалить брейкпоинт из JSON для цели по умолчанию.</summary>
    public static void RemoveBreakpointForDefaultTarget(string workspacePath, string filePath, int line)
    {
        if (line < 1 || string.IsNullOrEmpty(filePath))
            return;
        var path = Path.GetFullPath(filePath);
        var target = Path.GetFullPath(GetDefaultDebugTargetPath(workspacePath));
        var list = BreakpointsStorage.GetBreakpoints(workspacePath, target).ToList();
        list.RemoveAll(e =>
        {
            var ep = NormalizeEntryPath(workspacePath, e.File);
            return ep != null && string.Equals(ep, path, StringComparison.OrdinalIgnoreCase) && e.Line == line;
        });
        BreakpointsStorage.SetBreakpoints(workspacePath, target, list);
    }

    /// <summary>Переключить брейкпоинт в файле: если есть — удалить, иначе добавить в предпочитаемый target.</summary>
    public static void ToggleBreakpoint(string workspacePath, string filePath, int line)
    {
        if (line < 1) return;
        var path = Path.GetFullPath(filePath);
        var model = BreakpointsStorage.Load(workspacePath);
        var targetKey = GetPreferredTargetKey(workspacePath);
        if (string.IsNullOrEmpty(targetKey))
        {
            targetKey = Path.GetFullPath(GetDefaultDebugTargetPath(workspacePath));
            if (!model.Targets.ContainsKey(targetKey))
                model.Targets[targetKey] = [];
        }
        if (!model.Targets.TryGetValue(targetKey, out var list))
        {
            list = [];
            model.Targets[targetKey] = list;
        }
        var fileNorm = path;
        var removed = list.RemoveAll(e =>
        {
            var ep = NormalizeEntryPath(workspacePath, e.File);
            return ep != null && string.Equals(ep, fileNorm, StringComparison.OrdinalIgnoreCase) && e.Line == line;
        });
        if (removed == 0)
            list.Add(new BreakpointsStorage.BreakpointEntry(path, line));
        BreakpointsStorage.Save(workspacePath, model);
    }
}

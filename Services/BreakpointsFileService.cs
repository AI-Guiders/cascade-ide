using DotnetDebug.Core;

namespace CascadeIDE.Services;

/// <summary>
/// Операции IDE поверх <see cref="BreakpointsStorage"/> (тот же JSON, что у dotnet-debug-mcp).
/// «Bundled sample» — встроенная <c>samples/DebugTarget</c> для договорённостей MCP/доков; это не output «текущего» стартового проекта
/// (тот делается через <see cref="MsBuildDebugTargetResolver"/> и F5).
/// </summary>
public static class BreakpointsFileService
{
    public const string FileName = BreakpointsStorage.FileName;

    /// <summary>Отн. путь от корня monorepo к DLL встроенной sample-цели (после <c>dotnet build</c> в <c>samples/DebugTarget</c>).</summary>
    public static readonly string BundledSampleDebugTargetDllRelativeToRepoRoot =
        Path.Combine("samples", "DebugTarget", "bin", "Debug", "net10.0", "DebugTarget.dll");

    /// <summary>Отн. путь к той же DLL, когда workspace — только каталог проекта <c>…/samples/DebugTarget</c>.</summary>
    private static readonly string BundledSampleDebugTargetDllRelativeToProjectDir = Path.Combine("bin", "Debug", "net10.0", "DebugTarget.dll");

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
    /// Полный путь к DLL <b>встроенной</b> sample-цели <c>samples/DebugTarget</c>: ключ в JSON для MCP <c>set_breakpoint</c> и старт папки в диалоге выбора;
    /// не путать с output произвольного стартового проекта. Старт отладки F5/launch — с выбранным <c>target_path</c>.
    /// </summary>
    public static string GetBundledSampleDebugTargetDllPath(string workspacePath)
    {
        var root = GetWorkspaceRoot(workspacePath);
        if (IsBundledDebugTargetSampleProjectRoot(root))
            return Path.GetFullPath(Path.Combine(root, BundledSampleDebugTargetDllRelativeToProjectDir));
        return Path.GetFullPath(Path.Combine(root, BundledSampleDebugTargetDllRelativeToRepoRoot));
    }

    /// <summary>Корень workspace = каталог sample-проекта <c>DebugTarget</c> (не весь monorepo).</summary>
    private static bool IsBundledDebugTargetSampleProjectRoot(string workspaceRoot)
    {
        try
        {
            if (!string.Equals(Path.GetFileName(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar)), "DebugTarget", StringComparison.OrdinalIgnoreCase))
                return false;
            return File.Exists(Path.Combine(workspaceRoot, "DebugTarget.csproj"));
        }
        catch
        {
            return false;
        }
    }

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

    /// <summary>Выбрать target: сначала bundled sample (если есть ключ), иначе первый под workspace, иначе первый в списке.</summary>
    public static string? GetPreferredTargetKey(string workspacePath)
    {
        var ws = GetWorkspaceRoot(workspacePath);
        var model = BreakpointsStorage.Load(workspacePath);
        var preferred = Path.GetFullPath(GetBundledSampleDebugTargetDllPath(workspacePath));
        if (model.Targets.ContainsKey(preferred))
            return preferred;
        var key = model.Targets.Keys.FirstOrDefault(k => k.StartsWith(ws, StringComparison.OrdinalIgnoreCase));
        return key ?? model.Targets.Keys.FirstOrDefault();
    }

    /// <summary>Записать брейкпоинт в JSON для bundled sample-цели (<see cref="GetBundledSampleDebugTargetDllPath"/>), в т.ч. из MCP <c>set_breakpoint</c>.</summary>
    public static void SetBreakpointForBundledSampleTarget(string workspacePath, string filePath, int line, string? condition = null)
    {
        if (line < 1 || string.IsNullOrEmpty(filePath))
            return;
        var path = Path.GetFullPath(filePath);
        var target = Path.GetFullPath(GetBundledSampleDebugTargetDllPath(workspacePath));
        var list = BreakpointsStorage.GetBreakpoints(workspacePath, target).ToList();
        list.RemoveAll(e =>
        {
            var ep = NormalizeEntryPath(workspacePath, e.File);
            return ep != null && string.Equals(ep, path, StringComparison.OrdinalIgnoreCase) && e.Line == line;
        });
        list.Add(new BreakpointsStorage.BreakpointEntry(path, line, condition));
        BreakpointsStorage.SetBreakpoints(workspacePath, target, list);
    }

    /// <summary>Удалить брейкпоинт из JSON для той же bundled sample-цели.</summary>
    public static void RemoveBreakpointForBundledSampleTarget(string workspacePath, string filePath, int line)
    {
        if (line < 1 || string.IsNullOrEmpty(filePath))
            return;
        var path = Path.GetFullPath(filePath);
        var target = Path.GetFullPath(GetBundledSampleDebugTargetDllPath(workspacePath));
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
            targetKey = Path.GetFullPath(GetBundledSampleDebugTargetDllPath(workspacePath));
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

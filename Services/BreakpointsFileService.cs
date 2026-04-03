using DotnetDebug.Core;

namespace CascadeIDE.Services;

/// <summary>Операции IDE поверх <see cref="BreakpointsStorage"/> (тот же JSON, что у dotnet-debug-mcp).</summary>
public static class BreakpointsFileService
{
    public const string FileName = BreakpointsStorage.FileName;

    public static string GetFilePath(string workspacePath) => BreakpointsStorage.GetStorageFilePath(workspacePath);

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

    /// <summary>Выбрать target для добавления/удаления: первый, чей путь содержит workspace, иначе первый в списке.</summary>
    public static string? GetPreferredTargetKey(string workspacePath)
    {
        var ws = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(ws))
            ws = Path.GetDirectoryName(ws) ?? ws;
        var model = BreakpointsStorage.Load(workspacePath);
        var key = model.Targets.Keys.FirstOrDefault(k => k.StartsWith(ws, StringComparison.OrdinalIgnoreCase));
        return key ?? model.Targets.Keys.FirstOrDefault();
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
            targetKey = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(GetFilePath(workspacePath)) ?? "", "bin", "Debug", "net10.0", "win-x64", "CascadeIDE.exe"));
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

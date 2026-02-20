using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Services;

/// <summary>Чтение и запись .dotnet-debug-mcp-breakpoints.json (тот же формат, что у dotnet-debug-mcp).</summary>
public static class BreakpointsFileService
{
    public const string FileName = ".dotnet-debug-mcp-breakpoints.json";

    public record BreakpointEntry(string File, int Line, string? Condition = null);

    public record StorageModel(Dictionary<string, List<BreakpointEntry>> Targets)
    {
        public static StorageModel Empty() => new(new Dictionary<string, List<BreakpointEntry>>());
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string GetFilePath(string workspacePath)
    {
        var dir = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(dir))
            dir = Path.GetDirectoryName(dir) ?? dir;
        return Path.Combine(dir, FileName);
    }

    public static StorageModel Load(string workspacePath)
    {
        var path = GetFilePath(workspacePath);
        if (!File.Exists(path))
            return StorageModel.Empty();
        try
        {
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<StorageModel>(json, JsonOptions);
            if (model?.Targets != null)
                return model;
        }
        catch { /* ignore */ }
        return StorageModel.Empty();
    }

    public static void Save(string workspacePath, StorageModel model)
    {
        var path = GetFilePath(workspacePath);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(model, JsonOptions));
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
        var model = Load(workspacePath);
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
        var model = Load(workspacePath);
        var key = model.Targets.Keys.FirstOrDefault(k => k.StartsWith(ws, StringComparison.OrdinalIgnoreCase));
        return key ?? model.Targets.Keys.FirstOrDefault();
    }

    /// <summary>Переключить брейкпоинт в файле: если есть — удалить, иначе добавить в предпочитаемый target.</summary>
    public static void ToggleBreakpoint(string workspacePath, string filePath, int line)
    {
        if (line < 1) return;
        var path = Path.GetFullPath(filePath);
        var model = Load(workspacePath);
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
            list.Add(new BreakpointEntry(path, line));
        Save(workspacePath, model);
    }
}

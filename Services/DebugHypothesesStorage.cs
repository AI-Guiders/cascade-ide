using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>Один JSON на workspace: <c>.cascade-ide/debug-hypotheses.json</c> (ADR 0001).</summary>
public static class DebugHypothesesStorage
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string GetFilePath(string workspaceRootPath) =>
        Path.Combine(workspaceRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), ".cascade-ide", "debug-hypotheses.json");

    public static DebugHypothesesFileRoot Load(string workspaceRootPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRootPath))
            return new DebugHypothesesFileRoot();
        try
        {
            var path = GetFilePath(workspaceRootPath);
            if (!File.Exists(path))
                return new DebugHypothesesFileRoot();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DebugHypothesesFileRoot>(json, ReadOptions) ?? new DebugHypothesesFileRoot();
        }
        catch
        {
            return new DebugHypothesesFileRoot();
        }
    }

    public static void Save(string workspaceRootPath, DebugHypothesesFileRoot root)
    {
        if (string.IsNullOrWhiteSpace(workspaceRootPath))
            return;
        try
        {
            var dir = Path.Combine(workspaceRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), ".cascade-ide");
            Directory.CreateDirectory(dir);
            var path = GetFilePath(workspaceRootPath);
            File.WriteAllText(path, JsonSerializer.Serialize(root, WriteOptions));
        }
        catch
        {
            // как у других локальных JSON-артефактов IDE
        }
    }
}

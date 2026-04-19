using System.Text.Json;
using CascadeIDE.Contracts.Experimental.Capabilities;

namespace CascadeIDE.Services.Capabilities;

public static class CapabilityDump
{
    public static string GetDiagnosticsDirectory()
    {
        var baseDir = SettingsService.GetSettingsDirectory();
        var dir = Path.Combine(baseDir, "diagnostics");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string WriteCapabilityMapToFile(CapabilityMap map)
    {
        var dir = GetDiagnosticsDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var hash = string.IsNullOrWhiteSpace(map.Hash) ? "nohash" : map.Hash[..Math.Min(12, map.Hash.Length)].ToLowerInvariant();
        var file = Path.Combine(dir, $"capabilities-{stamp}-{hash}.json");
        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
        return file;
    }
}


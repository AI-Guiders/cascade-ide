#nullable enable
using System.Text.Json;

namespace CascadeIDE.SoftInstrument;

/// <summary>Glass MRU for Open-family (file / project / folder). LocalAppData JSON — not WitDB.</summary>
public static class GlassOpenRecentStore
{
    public const int MaxEntries = 12;

    public sealed record Entry(string Path, string Kind, long UtcTicks);

    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public static string StorePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cdp-glass",
            "open-recent.json");

    public static IReadOnlyList<Entry> List()
    {
        try
        {
            var path = StorePath;
            if (!File.Exists(path))
                return [];
            var raw = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<Entry>>(raw, JsonOpts) ?? [];
            return list
                .Where(e => !string.IsNullOrWhiteSpace(e.Path))
                .Take(MaxEntries)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    public static void Remember(string path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var full = path.Trim();
        var k = string.IsNullOrWhiteSpace(kind) ? "file" : kind.Trim().ToLowerInvariant();
        try
        {
            var list = List().Where(e => !string.Equals(e.Path, full, StringComparison.OrdinalIgnoreCase)).ToList();
            list.Insert(0, new Entry(full, k, DateTime.UtcNow.Ticks));
            if (list.Count > MaxEntries)
                list = list.Take(MaxEntries).ToList();

            var dir = Path.GetDirectoryName(StorePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(StorePath, JsonSerializer.Serialize(list, JsonOpts));
        }
        catch
        {
            // MRU is best-effort — never block open.
        }
    }
}

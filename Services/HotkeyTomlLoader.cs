namespace CascadeIDE.Services;

/// <summary>
/// Загрузка merged-словаря из штатного <c>Hotkeys/hotkeys.toml</c> и
/// <c>%LocalAppData%\CascadeIDE\hotkeys.toml</c> (оверлей поверх).
/// </summary>
public static class HotkeyTomlLoader
{
    public static Dictionary<string, string> LoadMergedDictionary()
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var basePath = Path.Combine(AppContext.BaseDirectory, "Hotkeys", "hotkeys.toml");
        MergeFromFile(merged, basePath, "Hotkeys/hotkeys.toml");
        var userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CascadeIDE",
            "hotkeys.toml");
        MergeFromFile(merged, userPath, embeddedRelativeFallback: null);
        return merged;
    }

    internal static void MergeFromFile(
        Dictionary<string, string> target,
        string path,
        string? embeddedRelativeFallback = null)
    {
        string? text = null;
        if (File.Exists(path))
            text = File.ReadAllText(path);
        else if (embeddedRelativeFallback is not null && BundledAppContent.TryReadEmbeddedText(embeddedRelativeFallback, out var emb))
            text = emb;
        if (text is null)
            return;
        try
        {
            var parsed = CascadeTomlSerializer.Deserialize<Dictionary<string, string>>(text);
            if (parsed is null)
                return;
            foreach (var kv in parsed)
                target[kv.Key] = kv.Value.Trim();
        }
        catch
        {
            // игнорируем битый файл — остаётся предыдущий мердж
        }
    }
}

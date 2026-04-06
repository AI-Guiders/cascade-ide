using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>
/// Загрузка подсказок хоткеев: штатный <c>Hotkeys/hotkeys.toml</c> + пользовательский
/// <c>%LocalAppData%\CascadeIDE\hotkeys.toml</c> (мердж поверх).
/// </summary>
public sealed class HotkeyGestureMap
{
    private readonly ImmutableDictionary<string, string> _byCommandId;

    private HotkeyGestureMap(ImmutableDictionary<string, string> byCommandId) =>
        _byCommandId = byCommandId;

    /// <summary>Строка жеста для отображения рядом с командой, или null.</summary>
    public string? GetDisplayHint(string commandId) =>
        _byCommandId.TryGetValue(commandId, out var g) ? g : null;

    public static HotkeyGestureMap Load()
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var basePath = Path.Combine(AppContext.BaseDirectory, "Hotkeys", "hotkeys.toml");
        MergeFromFile(merged, basePath);
        var userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CascadeIDE",
            "hotkeys.toml");
        MergeFromFile(merged, userPath);
        return new HotkeyGestureMap(merged.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));
    }

    private static void MergeFromFile(Dictionary<string, string> target, string path)
    {
        if (!File.Exists(path))
            return;
        try
        {
            var text = File.ReadAllText(path);
            var parsed = CascadeTomlSerializer.Deserialize<Dictionary<string, string>>(text);
            if (parsed is null)
                return;
            foreach (var kv in parsed)
                target[kv.Key] = kv.Value.Trim();
        }
        catch
        {
            // игнорируем битый пользовательский файл — остаётся штатный мердж
        }
    }
}


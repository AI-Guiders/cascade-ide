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
        var merged = HotkeyTomlLoader.LoadMergedDictionary();
        return new HotkeyGestureMap(merged.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));
    }
}


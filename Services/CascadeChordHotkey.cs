#nullable enable
using Avalonia.Input;

namespace CascadeIDE.Services;

/// <summary>Разбор корневого жеста CascadeChord из merged <c>hotkeys.toml</c> (ADR 0060).</summary>
public static class CascadeChordHotkey
{
    public const string TomlKey = "cascade_chord";
    public const string DefaultGestureString = "Ctrl+K";

    /// <summary>
    /// Жест для первого шага аккорда: ключ <see cref="TomlKey"/> или <see cref="DefaultGestureString"/>;
    /// при неразборчивой строке — повторный разбор <see cref="DefaultGestureString"/>.
    /// </summary>
    public static KeyGesture ResolveRootGesture(IReadOnlyDictionary<string, string> mergedMap)
    {
        var s = DefaultGestureString;
        if (mergedMap.TryGetValue(TomlKey, out var v) && !string.IsNullOrWhiteSpace(v))
            s = v.Trim();
        try
        {
            return KeyGesture.Parse(s);
        }
        catch
        {
            return KeyGesture.Parse(DefaultGestureString);
        }
    }

    /// <summary>
    /// Совпадение корня аккорда с событием: сначала <see cref="KeyGesture.Matches(KeyEventArgs)"/>,
    /// затем fallback по <see cref="KeyEventArgs.PhysicalKey"/> = <see cref="PhysicalKey.K"/> при тех же
    /// <see cref="KeyEventArgs.KeyModifiers"/>, что у <paramref name="resolved"/>.
    /// Иначе при русской (и др.) раскладке <c>e.Key</c> ≠ <c>Key.K</c>, хотя нажата та же физическая клавиша — корень не срабатывал.
    /// </summary>
    public static bool RootGestureMatches(KeyGesture resolved, KeyEventArgs e)
    {
        if (resolved.Matches(e))
            return true;
        return MatchesPhysicalKeyFallback(resolved, e.PhysicalKey, e.KeyModifiers);
    }

    /// <summary>Вторая ветка <see cref="RootGestureMatches"/> — для тестов и ясности контракта.</summary>
    public static bool MatchesPhysicalKeyFallback(KeyGesture resolved, PhysicalKey physicalKey, KeyModifiers modifiers) =>
        physicalKey == PhysicalKey.K && modifiers == resolved.KeyModifiers;
}

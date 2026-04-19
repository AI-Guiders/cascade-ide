#nullable enable
using Avalonia.Input;

namespace CascadeIDE.Services;

/// <summary>
/// Сопоставление <see cref="KeyGesture"/> с <see cref="KeyEventArgs"/> с учётом раскладки и лишних битов в <see cref="KeyEventArgs.KeyModifiers"/>
/// (в т.ч. Avalonia 11+): встроенный <see cref="KeyGesture.Matches(KeyEventArgs)"/> требует полного совпадения модификаторов и совпадения <see cref="KeyEventArgs.Key"/> с жестом.
/// Для буквенных хоткеев (Ctrl+Q, Ctrl+K, …) дополнительно используется <see cref="PhysicalKeyExtensions.ToQwertyKey(Avalonia.Input.PhysicalKey)"/>, чтобы позиция на QWERTY совпадала с жестом при нелатинской раскладке.
/// </summary>
public static class KeyGestureChordMatching
{
    private const KeyModifiers ChordModifierMask =
        KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Meta;

    public static KeyModifiers NormalizeChordModifiers(KeyModifiers m) => m & ChordModifierMask;

    /// <summary>Та же логика, что у <see cref="KeyGesture"/> для цифровой клавиатуры.</summary>
    public static Key NormalizeNumPadAlias(Key key) =>
        key switch
        {
            Key.Add => Key.OemPlus,
            Key.Subtract => Key.OemMinus,
            Key.Decimal => Key.OemPeriod,
            _ => key,
        };

    /// <summary>
    /// Сначала строгий <see cref="KeyGesture.Matches(KeyEventArgs)"/>; иначе совпадение по нормализованным модификаторам
    /// и клавише (включая non-Latin: та же физическая K, что и у жеста <c>Key.K</c>).
    /// </summary>
    public static bool Matches(KeyGesture gesture, KeyEventArgs e)
    {
        if (gesture.Matches(e))
            return true;

        if (NormalizeChordModifiers(e.KeyModifiers) != NormalizeChordModifiers(gesture.KeyModifiers))
            return false;

        if (NormalizeNumPadAlias(e.Key) == NormalizeNumPadAlias(gesture.Key))
            return true;

        // Нелатинские раскладки: e.Key — по текущей раскладке, жест в TOML — как на QWERTY (Ctrl+Q, Ctrl+K, …).
        // См. KeyEventArgs.Key / PhysicalKey в документации Avalonia; ToQwertyKey — позиция на US QWERTY → Key.
        var qwertyKey = e.PhysicalKey.ToQwertyKey();
        if (qwertyKey != Key.None &&
            NormalizeNumPadAlias(qwertyKey) == NormalizeNumPadAlias(gesture.Key))
            return true;

        return false;
    }
}

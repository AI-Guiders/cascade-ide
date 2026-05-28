using Avalonia.Input;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Соответствие клавиш главного окна символам мелодии CascadeChord.</summary>
[PresentationProjection("cascade-chord-melody-keys")]
public static class CascadeChordMelodyKeyMap
{
    /// <summary>
    /// Строчные латиница/цифры и символы для параметрических хвостов (<c>:</c>, <c>/</c>, <c>.</c>, <c>-</c>).
    /// Буквы — по <see cref="Key"/> (как в ADR 0060 зеркало HUD); «:» и пробел дополняются
    /// <see cref="PhysicalKey"/>, чтобы раскладка не ломала <c>:</c> (например RU: Shift+физ. 6).
    /// </summary>
    public static bool TryMapChordMelodyGlyph(Key key, KeyModifiers modifiers, PhysicalKey physicalKey, out char ch)
    {
        ch = default;
        var shift = modifiers.HasFlag(KeyModifiers.Shift);
        // В chord-последовательностях Ctrl часто продолжают держать (в стиле VSCode: Ctrl+K затем /).
        // Поэтому Ctrl здесь игнорируем; а вот Alt/Meta ломают ввод и считаются "не мелодией".
        if (modifiers.HasFlag(KeyModifiers.Alt) || modifiers.HasFlag(KeyModifiers.Meta))
            return false;

        if (key == Key.Space)
        {
            ch = ' ';
            return true;
        }

        // Физ. клавиша «;» (US справа от L): без Shift — «;» в хвост (разделитель в int_chain, см. TOML); со Shift — «:».
        // Так аккорд обходит глючный Shift у Avalonia для «:».
        if (physicalKey == PhysicalKey.Semicolon)
        {
            ch = shift ? ':' : ';';
            return true;
        }

        // «:» — RU и др.: часто Shift+физ. цифра 6 (на US той же клавише — ^; в хвосте мелодии нужен ':').
        if (physicalKey == PhysicalKey.Digit6 && shift)
        {
            ch = ':';
            return true;
        }

        if (TryMapToChar(key, out ch))
            return true;

        return key switch
        {
            // Windows US часто даёт VK_OEM_1 как Key.Oem1; «;» / «:» — те же позиции, что и PhysicalKey.Semicolon.

            Key.OemSemicolon when shift =>
                WithChar(':', out ch),
            Key.OemSemicolon when !shift =>
                WithChar(';', out ch),
            Key.Oem1 when shift =>
                WithChar(':', out ch),
            Key.Oem1 when !shift =>
                WithChar(';', out ch),
            Key.OemPeriod when !shift =>
                WithChar('.', out ch),

            Key.Oem2 or Key.Divide =>
                WithChar('/', out ch),
            Key.OemMinus or Key.Subtract =>
                shift ? WithChar('_', out ch) : WithChar('-', out ch),
            Key.OemPlus or Key.Add =>
                shift ? WithChar('+', out ch) : WithChar('=', out ch),
            _ => false,
        };

        static bool WithChar(char c, out char o)
        {
            o = c;
            return true;
        }
    }

    public static bool TryMapToChar(Key key, out char ch)
    {
        ch = default;
        if (key >= Key.A && key <= Key.Z)
        {
            ch = (char)('a' + (key - Key.A));
            return true;
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            ch = (char)('0' + (key - Key.D0));
            return true;
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            ch = (char)('0' + (key - Key.NumPad0));
            return true;
        }

        return false;
    }
}

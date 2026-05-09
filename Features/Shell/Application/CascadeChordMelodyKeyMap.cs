using Avalonia.Input;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Соответствие клавиш главного окна символам мелодии CascadeChord.</summary>
public static class CascadeChordMelodyKeyMap
{
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

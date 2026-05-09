namespace CascadeIDE.Features.Search.Application;

/// <summary>Лимиты режима палитры «перейти к…» (<c>f:/t:/m:/x:</c>).</summary>
public static class CommandPaletteGoToLimits
{
    public const int MaxFiles = 100;
    public const int MaxRipgrepMatches = 80;
    public const int RipgrepDebounceMs = 220;
}

namespace CascadeIDE.Services.ChordNotation;

/// <summary>
/// Слой нормализации: сырые шаги Vim-стиля → <see cref="NormalizedKeySequence"/>.
/// </summary>
public static class ChordSemanticNormalizer
{
    public static NormalizedKeySequence FromVimSteps(IReadOnlyList<ChordNotationStep> steps)
    {
        if (steps.Count == 0)
            return NormalizedKeySequence.Empty;

        var list = new List<NormalizedSequenceStep>(steps.Count);
        foreach (var s in steps)
        {
            switch (s)
            {
                case ChordNotationChordStep ch:
                    list.Add(new NormalizedChordStep(MapVimPrefixes(ch.ModifierPrefixes), NormalizeKeySymbol(ch.Key)));
                    break;
                case ChordNotationPlainStep pl:
                    list.Add(new NormalizedPlainKeyStep(NormalizeKeySymbol(pl.Token)));
                    break;
            }
        }

        return new NormalizedKeySequence(list);
    }

    private static ChordModifierKeys MapVimPrefixes(IReadOnlyList<string> prefixes)
    {
        ChordModifierKeys m = 0;
        foreach (var p in prefixes)
        {
            m |= p switch
            {
                "C-" => ChordModifierKeys.Control,
                "M-" or "A-" or "Alt-" => ChordModifierKeys.Alt,
                "S-" => ChordModifierKeys.Shift,
                "D-" => ChordModifierKeys.Meta,
                _ => 0
            };
        }

        return m;
    }

    /// <summary>Единый вид символа клавиши: буквы одной буквы — верхний регистр; остальное без лишних правок.</summary>
    public static string NormalizeKeySymbol(string key)
    {
        if (key.Length == 1 && char.IsLetter(key[0]))
            return key.ToUpperInvariant();
        return key;
    }
}

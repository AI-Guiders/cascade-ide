using System.Diagnostics.CodeAnalysis;

namespace CascadeIDE.Services.ChordNotation;

/// <summary>
/// Синтаксический слой «как в UI / hotkeys»: <c>Ctrl+K</c>, <c>Ctrl + Shift + P</c>, <c>⌘K</c>, последовательности через пробел.
/// Разбор не смешивает платформенные строки с грамматикой Vim — результат тот же <see cref="NormalizedKeySequence"/>, что и после <see cref="ChordSemanticNormalizer.FromVimSteps"/>.
/// </summary>
public static class KeyGestureChordSyntax
{
    private const char CommandKeySymbol = '\u2318'; // ⌘

    /// <summary>Пробельно-разделённые токены: каждый либо аккорд с «+», либо префикс ⌘, либо plain.</summary>
    public static bool TryParseToNormalized(string? input, out NormalizedKeySequence? sequence, out string error)
    {
        sequence = null;
        error = "";

        if (string.IsNullOrEmpty(input))
        {
            sequence = NormalizedKeySequence.Empty;
            return true;
        }

        var trimmed = CollapseSpacesAroundPlus(input.Trim());
        var tokens = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var steps = new List<NormalizedSequenceStep>(tokens.Length);

        foreach (var raw in tokens)
        {
            if (!TryParseOneToken(raw, out var step, out var err))
            {
                error = err;
                return false;
            }

            steps.Add(step);
        }

        sequence = new NormalizedKeySequence(steps);
        return true;
    }

    /// <summary>Схлопывает пробелы вокруг <c>+</c>, чтобы <c>Ctrl + Shift + P</c> оставался одним токеном при split по пробелам.</summary>
    private static string CollapseSpacesAroundPlus(string input)
    {
        if (!input.Contains('+', StringComparison.Ordinal))
            return input;
        var parts = input.Split('+');
        for (var i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim();
        return string.Join("+", parts);
    }

    private static bool TryParseOneToken(string token, [NotNullWhen(true)] out NormalizedSequenceStep? step, out string error)
    {
        error = "";
        step = null;
        token = token.Trim();
        if (token.Length == 0)
        {
            error = "Пустой токен.";
            return false;
        }

        // ⌘K без плюса
        if (token.Length >= 2 && token[0] == CommandKeySymbol)
        {
            var rest = token[1..];
            step = new NormalizedChordStep(ChordModifierKeys.Meta, ChordSemanticNormalizer.NormalizeKeySymbol(rest));
            return true;
        }

        if (!token.Contains('+', StringComparison.Ordinal))
        {
            step = new NormalizedPlainKeyStep(ChordSemanticNormalizer.NormalizeKeySymbol(token));
            return true;
        }

        var parts = token.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            error = $"Ожидалось хотя бы Mod+Key, получено: «{token}».";
            return false;
        }

        ChordModifierKeys mods = 0;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var mk = MapModifierWord(parts[i]);
            if (mk == null)
            {
                error = $"Неизвестный модификатор: «{parts[i]}».";
                return false;
            }

            mods |= mk.Value;
        }

        var key = ChordSemanticNormalizer.NormalizeKeySymbol(parts[^1]);
        step = new NormalizedChordStep(mods, key);
        return true;
    }

    private static ChordModifierKeys? MapModifierWord(string word)
    {
        if (word.Length == 1 && word[0] == CommandKeySymbol)
            return ChordModifierKeys.Meta;

        var w = word.Trim();
        if (w.Length == 0)
            return null;

        return w.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => ChordModifierKeys.Control,
            "ALT" or "OPTION" => ChordModifierKeys.Alt,
            "SHIFT" => ChordModifierKeys.Shift,
            "META" or "CMD" or "COMMAND" or "WIN" or "SUPER" => ChordModifierKeys.Meta,
            _ => null
        };
    }
}

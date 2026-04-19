using Eto.Parse;

namespace CascadeIDE.Services.ChordNotation;

/// <summary>
/// Разбор нотации из <c>docs/chord-notation-cascadeide.md</c> (EBNF там же).
/// Грамматика задаётся через <see cref="Eto.Parse"/> (fluent API, один-в-один с EBNF).
/// </summary>
public static class ChordNotationParser
{
    /// <summary>Парсит строку (например <c>&lt;C-k&gt; s p</c>). Пустая строка — успех с нулём шагов.</summary>
    public static ChordNotationParseResult Parse(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return ChordNotationParseResult.Ok(Array.Empty<ChordNotationStep>());

        var trimmed = input.Trim();
        var gm = ChordNotationGrammar.Instance.Match(trimmed);
        if (!gm.Success)
            return ChordNotationParseResult.Fail(FormatGrammarError(gm, trimmed));

        var tokens = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var steps = new List<ChordNotationStep>(tokens.Length);
        foreach (var t in tokens)
        {
            if (t.Length >= 2 && t[0] == '<' && t[^1] == '>')
            {
                steps.Add(ParseBracketInner(t.AsSpan(1, t.Length - 2)));
                continue;
            }

            steps.Add(new ChordNotationPlainStep(t));
        }

        return ChordNotationParseResult.Ok(steps);
    }

    /// <summary>Vim-стиль (<c>&lt;C-k&gt; s p</c>) → нормализованная модель (см. два слоя в доке).</summary>
    public static bool TryParseVimToNormalized(string? input, out NormalizedKeySequence? sequence, out string error)
    {
        sequence = null;
        var r = Parse(input);
        if (!r.IsSuccess)
        {
            error = r.Error;
            return false;
        }

        sequence = ChordSemanticNormalizer.FromVimSteps(r.Steps);
        error = "";
        return true;
    }

    private static ChordNotationChordStep ParseBracketInner(ReadOnlySpan<char> inner)
    {
        var mods = new List<string>(4);
        while (!inner.IsEmpty)
        {
            if (inner.StartsWith("Alt-", StringComparison.Ordinal))
            {
                mods.Add("Alt-");
                inner = inner[4..];
                continue;
            }

            if (inner.StartsWith("C-", StringComparison.Ordinal))
            {
                mods.Add("C-");
                inner = inner[2..];
                continue;
            }

            if (inner.StartsWith("M-", StringComparison.Ordinal))
            {
                mods.Add("M-");
                inner = inner[2..];
                continue;
            }

            if (inner.StartsWith("A-", StringComparison.Ordinal))
            {
                mods.Add("A-");
                inner = inner[2..];
                continue;
            }

            if (inner.StartsWith("S-", StringComparison.Ordinal))
            {
                mods.Add("S-");
                inner = inner[2..];
                continue;
            }

            if (inner.StartsWith("D-", StringComparison.Ordinal))
            {
                mods.Add("D-");
                inner = inner[2..];
                continue;
            }

            break;
        }

        var key = inner.ToString();
        if (key.Length == 0)
            throw new InvalidOperationException("Chord notation: пустой key внутри <…> (грамматика должна была отсечь).");

        return new ChordNotationChordStep(mods, key);
    }

    private static string FormatGrammarError(GrammarMatch gm, string text)
    {
        var idx = gm.ErrorIndex >= 0 ? gm.ErrorIndex : 0;
        var tail = idx < text.Length ? text[idx..] : "";
        tail = tail.Length > 24 ? tail[..24] + "…" : tail;
        return $"Chord notation: разбор остановился на позиции {idx} (остаток: «{tail}»).";
    }
}

/// <param name="IsSuccess">Успех полного разбора.</param>
/// <param name="Steps">Шаги последовательности (после успеха).</param>
/// <param name="Error">Сообщение при неуспехе.</param>
public readonly record struct ChordNotationParseResult(bool IsSuccess, IReadOnlyList<ChordNotationStep> Steps, string Error)
{
    public static ChordNotationParseResult Ok(IReadOnlyList<ChordNotationStep> steps) =>
        new(true, steps, "");

    public static ChordNotationParseResult Fail(string message) =>
        new(false, Array.Empty<ChordNotationStep>(), message);
}

public abstract record ChordNotationStep;

/// <summary>Совпадающее нажатие: <c>&lt;C-k&gt;</c>, <c>&lt;C-M-n&gt;</c>.</summary>
public sealed record ChordNotationChordStep(IReadOnlyList<string> ModifierPrefixes, string Key) : ChordNotationStep;

/// <summary>Токен без скобок: <c>m</c>, <c>Esc</c>, <c>L1</c>.</summary>
public sealed record ChordNotationPlainStep(string Token) : ChordNotationStep;

using System.Text;
using CascadeIDE.Models.Shell;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Чистая логика текста и фильтрации CascadeChord (ADR 0060) без состояния VM.</summary>
public static class CascadeChordPresentationProjection
{
    public static string NormalizeMelodyInput(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static string TruncateChordTitle(string s, int maxChars = 52)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        s = s.Trim();
        return s.Length <= maxChars ? s : s[..(maxChars - 1)] + "…";
    }

    public static IReadOnlyList<(string Alias, string CommandId)> FilterEligibleMatches(string tailNormalized) =>
        IntentMelodyAliases.FilterByTailPrefix(tailNormalized)
            .Where(m => ParametricIntentMelody.IsChordEligibleAlias(m.Alias))
            .ToList();

    public static IReadOnlyList<CascadeChordOverlaySuggestion> BuildSuggestionRows(
        bool isAwaitMelodyTail,
        string tailNormalized,
        int maxItems)
    {
        if (!isAwaitMelodyTail)
            return [];

        var matches = FilterEligibleMatches(tailNormalized);
        return matches
            .Take(maxItems)
            .Select(m => new CascadeChordOverlaySuggestion(
                m.Alias,
                TruncateChordTitle(IdeCommandDocDisplay.ShortTitleForCommandId(m.CommandId))))
            .ToList();
    }

    public static string BuildOverlayHint(
        bool isAwaitMelodyTail,
        string melodyTail,
        int timeoutSeconds,
        IReadOnlyList<(string Alias, string CommandId)> eligibleMatches)
    {
        var buf = melodyTail;
        var bufLine = string.IsNullOrEmpty(buf)
            ? "Набрано: (пусто) — тот же хвост, что после c: в палитре (например cps, cs, so)."
            : $"Набрано: «{buf}»";

        if (!isAwaitMelodyTail)
        {
            return "CascadeChord\n" +
                   "  Esc — отмена · таймаут " + timeoutSeconds + " с";
        }

        var matchLine = eligibleMatches.Count == 0
            ? "Нет alias с таким префиксом."
            : "Совпадения: " + string.Join(", ", eligibleMatches.Select(m => m.Alias));

        return "CascadeChord · мелодия (как c:… без префикса и без Enter, если alias однозначен)\n" +
               bufLine + "\n" +
               matchLine + "\n" +
               "  Enter — выполнить, если хвост — точный alias (нужно при gs vs gsu)\n" +
               "  Backspace — стереть символ · Esc — отмена · таймаут " + timeoutSeconds + " с\n" +
               "Палитра Ctrl+Q, c: — тот же каталог.";
    }
}

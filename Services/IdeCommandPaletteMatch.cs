using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

/// <summary>Поиск и ранжирование записей палитры команд (подстрока + простой fuzzy по подпоследовательности).</summary>
public static class IdeCommandPaletteMatch
{
    /// <summary>Возвращает упорядоченный список записей для отображения.</summary>
    public static IReadOnlyList<IdeCommandPaletteCatalog.Entry> FilterAndRank(
        IReadOnlyList<IdeCommandPaletteCatalog.Entry> all,
        string query)
    {
        var q = query.Trim();
        if (string.IsNullOrEmpty(q))
        {
            return all.OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var scored = new List<(IdeCommandPaletteCatalog.Entry Entry, int Score)>();
        foreach (var e in all)
        {
            var hay = BuildHaystack(e);
            var score = MatchScore(hay, q, e.Title);
            if (score >= 0)
                scored.Add((e, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Entry)
            .ToList();
    }

    public static bool IsEntryAvailable(IdeCommandPaletteCatalog.Entry e, UiModeFamily family)
    {
        if (e.AllowedFamilies is not { } allowed || allowed.IsDefaultOrEmpty)
            return true;
        foreach (var f in allowed)
        {
            if (f == family)
                return true;
        }

        return false;
    }

    /// <summary>Подсказка для недоступной строки (UX: «Недоступно в режиме …»).</summary>
    public static string? UnavailableHint(IdeCommandPaletteCatalog.Entry e, UiModeFamily current)
    {
        if (IsEntryAvailable(e, current))
            return null;
        if (e.AllowedFamilies is { } fam && !fam.IsDefaultOrEmpty && fam.Length == 1)
            return $"Только в режиме {FormatFamily(fam[0])}";
        return "Недоступно в текущем UI-режиме";
    }

    private static string FormatFamily(UiModeFamily f) =>
        f switch
        {
            UiModeFamily.Focus => "Focus",
            UiModeFamily.Editor => "Editor",
            UiModeFamily.Balanced => "Balanced",
            UiModeFamily.Power => "Power",
            UiModeFamily.AgentChat => "Agent Chat",
            UiModeFamily.Debug => "Debug",
            UiModeFamily.Flight => "Flight",
            _ => f.ToString(),
        };

    private static string BuildHaystack(IdeCommandPaletteCatalog.Entry e) =>
        $"{e.Title} {e.Category} {e.CommandId} {e.PaletteId}";

    /// <summary>Чем выше, тем лучше совпадение; -1 — не подходит.</summary>
    private static int MatchScore(string haystack, string query, string titleForWordBonus)
    {
        if (query.Length == 0)
            return 0;

        var idx = haystack.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var baseScore = 10_000_000 - idx * 100;
            if (TitleHasWordStartingWith(titleForWordBonus, query))
                baseScore += 50_000;
            return baseScore;
        }

        if (!IsSubsequenceIgnoreCase(haystack, query))
            return -1;

        var spread = SubsequenceSpread(haystack, query);
        var fuzzy = 5_000_000 - spread * 1000 - Math.Min(haystack.Length, 500);
        if (TitleHasWordStartingWith(titleForWordBonus, query))
            fuzzy += 25_000;
        return fuzzy;
    }

    private static bool TitleHasWordStartingWith(string title, string q)
    {
        foreach (var word in title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsSubsequenceIgnoreCase(string hay, string q)
    {
        var hi = 0;
        for (var qi = 0; qi < q.Length; qi++)
        {
            var qc = char.ToLowerInvariant(q[qi]);
            while (hi < hay.Length && char.ToLowerInvariant(hay[hi]) != qc)
                hi++;
            if (hi >= hay.Length)
                return false;
            hi++;
        }

        return true;
    }

    private static int SubsequenceSpread(string hay, string q)
    {
        var hi = 0;
        var spread = 0;
        var last = -1;
        for (var qi = 0; qi < q.Length; qi++)
        {
            var qc = char.ToLowerInvariant(q[qi]);
            while (hi < hay.Length && char.ToLowerInvariant(hay[hi]) != qc)
                hi++;
            if (hi >= hay.Length)
                return int.MaxValue;
            if (last >= 0)
                spread += hi - last;
            last = hi;
            hi++;
        }

        return spread;
    }
}

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using CascadeIDE.Models.Editor;

namespace CascadeIDE.Services;

/// <summary>
/// Параметрические формы Command Melody (ADR 0081, каталог ADR 0109 + <c>[[tail_wire_class]]</c>).
/// </summary>
public static class ParametricIntentMelody
{
    /// <param name="Lines">Инвариантный 1-based диапазон; см. ADR 0111.</param>
    public sealed record ParsedLineRange(string Alias, string DisplayTail, LineRange Lines);

    /// <summary>Палитровый ключ подсказки: <see cref="MelodyRootEntry.PaletteHintSlug"/> иначе slug.</summary>
    public static string ResolvePaletteHintKey(string melodySlug)
    {
        if (!IntentMelodyCatalog.TryGetRoot(melodySlug, out var e))
            return melodySlug.Trim().ToLowerInvariant();

        var hint = e.PaletteHintSlug?.Trim();
        return string.IsNullOrEmpty(hint) ? e.Slug : hint.Trim().ToLowerInvariant();
    }

    public static string BuildAliasUsageHintForPalette(string melodySlug) =>
        BuildAliasUsageHint(ResolvePaletteHintKey(melodySlug));

    public static string BuildAliasUsageCategoryForPalette(string melodySlug) =>
        BuildAliasUsageCategory(ResolvePaletteHintKey(melodySlug));

    public static bool IsPaletteOnlyAlias(string alias)
    {
        var a = alias.Trim();
        return !string.IsNullOrWhiteSpace(a)
               && IntentMelodyCatalog.TryGetRoot(a, out var e)
               && e.ShowUsageHintIfBareSlug;
    }

    public static bool IsParametricMelodyBaseAlias(string alias) =>
        !string.IsNullOrWhiteSpace(alias)
        && IntentMelodyCatalog.TryGetRoot(alias.Trim(), out var e)
        && e.Shape == IntentMelodyShape.Parametric;

    public static bool IsChordEligibleAlias(string alias) =>
        IsParametricMelodyBaseAlias(alias) || !IsPaletteOnlyAlias(alias);

    public static bool IsParametricChordTailPrefix(string tailNormalizedLower)
    {
        if (string.IsNullOrEmpty(tailNormalizedLower))
            return false;

        foreach (var e in IntentMelodyAliases.GetCatalogSnapshot().Roots.Values)
        {
            if (e.Shape != IntentMelodyShape.Parametric)
                continue;

            if (string.IsNullOrWhiteSpace(e.WireClass)
                || !IntentMelodyCatalog.TryGetTailWireClass(e.WireClass, out var wire))
                continue;

            if (wire.Kind == TailWireKind.SingleRemainder
                && IntentMelodyTailSemantics.HasUrlSlot(e.TailSignature))
            {
                var w = e.Slug;
                if (tailNormalizedLower.Length <= w.Length
                    && w.StartsWith(tailNormalizedLower, StringComparison.Ordinal))
                    return true;

                if (string.Equals(tailNormalizedLower, w, StringComparison.Ordinal))
                    return true;

                if (tailNormalizedLower.StartsWith(w + ':', StringComparison.Ordinal))
                    return true;

                continue;
            }

            if (wire.Kind == TailWireKind.DelimitedSlots)
            {
                var b = e.Slug;
                if (tailNormalizedLower.Length <= b.Length && b.StartsWith(tailNormalizedLower, StringComparison.Ordinal))
                    return true;

                if (!TailTryStripRemainderAfterSlug(tailNormalizedLower, e, wire, false, out _, out _, out var rem))
                    continue;

                var expectedNumericSlots = IntentMelodyTailSemantics.CountDelimitedNumericSlots(e.TailSignature);
                return string.IsNullOrEmpty(rem)
                       || SegmentPrefixIsIncompleteOrFilled(wire, rem, expectedNumericSlots, requireFull: false);
            }
        }

        return false;
    }

    internal static bool ChordDefersInstantExecuteForMelodyRoot(in MelodyRootEntry entry)
    {
        if (entry.Shape != IntentMelodyShape.Parametric)
            return false;

        var m = NormalizeChordCommit(entry.ChordCommit);
        return m is not "immediate" and not "instant";
    }

    /// <remarks><c>chord_commit</c> из TOML (<see cref="MelodyRootEntry.ChordCommit"/>).</remarks>
    internal static bool ChordDefersInstantExecuteForExactAlias(string exactAlias) =>
        IntentMelodyCatalog.TryGetRoot(exactAlias.Trim(), out var e) && ChordDefersInstantExecuteForMelodyRoot(e);

    public static bool TryParseWebAiPortalMelodyTail(string tailNormalized, out string? urlPayloadTrimmed) =>
        TryParseWebAiPortalMelodyTail(tailNormalized, out _, out urlPayloadTrimmed);

    /// <summary>Разбор параметрики вида один «остаток» после первого <c>:</c> (URL-слоты из каталога).</summary>
    public static bool TryParseWebAiPortalMelodyTail(
        string tailNormalized,
        out string melodySlugLower,
        out string? urlPayloadTrimmed)
    {
        melodySlugLower = "";
        urlPayloadTrimmed = null;

        if (string.IsNullOrEmpty(tailNormalized))
            return false;

        tailNormalized = tailNormalized.Trim();

        foreach (var entry in EnumerateSingleRemainderUrlRoots())
        {
            var slug = entry.Slug;
            if (string.IsNullOrWhiteSpace(entry.WireClass)
                || !IntentMelodyCatalog.TryGetTailWireClass(entry.WireClass, out var wire))
                continue;
            if (wire.Kind != TailWireKind.SingleRemainder || !IntentMelodyTailSemantics.HasUrlSlot(entry.TailSignature))
                continue;

            if (string.Equals(tailNormalized, slug, StringComparison.Ordinal))
            {
                melodySlugLower = slug;
                urlPayloadTrimmed = "";
                return true;
            }

            var prefix = slug + ":";
            if (!tailNormalized.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            melodySlugLower = slug;
            urlPayloadTrimmed = tailNormalized.Length > prefix.Length
                ? tailNormalized[prefix.Length..].Trim()
                : "";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Единая точка: успешно собрать <c>command_id</c> и JSON-args для параметрики палитры/аккорда —
    /// остаток URL (<see cref="TailWireKind.SingleRemainder"/> + url-слот) или два int на проводе
    /// (<see cref="TailWireKind.DelimitedSlots"/>), см. каталог ADR 0109.
    /// </summary>
    /// <param name="displayTailForPaletteRow">Хвост для строки палитры: исходный ввод в URL-ветке или <see cref="ParsedLineRange.DisplayTail"/> для диапазона.</param>
    public static bool TryResolveParametricExecution(
        string melodyTailNormalized,
        string? currentFilePath,
        string editorText,
        out string commandId,
        out string? argsJson,
        out string displayTailForPaletteRow)
    {
        commandId = "";
        argsJson = null;
        displayTailForPaletteRow = "";

        var tail = (melodyTailNormalized ?? "").Trim();
        if (tail.Length == 0)
            return false;

        if (TryParseWebAiPortalMelodyTail(tail, out var melodySlug, out var urlPayload))
        {
            var webCmd = IntentMelodyAliases.TryResolveExactCommandId(melodySlug);
            if (string.IsNullOrEmpty(webCmd))
                return false;

            commandId = webCmd;
            var url = (urlPayload ?? "").Trim();
            argsJson = string.IsNullOrEmpty(url) ? null : JsonSerializer.Serialize(new { url });
            displayTailForPaletteRow = tail;
            return true;
        }

        if (!TryParseLineRangeTail(tail, out var parsed) || parsed is null)
            return false;

        if (!TryBuildExecutionArgs(parsed, currentFilePath, editorText, out var lineCmdId, out var lineArgsJson,
                out _))
            return false;

        commandId = lineCmdId;
        argsJson = lineArgsJson;
        displayTailForPaletteRow = parsed.DisplayTail;
        return true;
    }

    public static bool TryParseLineRangeTail(string tailNormalized, out ParsedLineRange? parsed)
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(tailNormalized))
            return false;

        tailNormalized = tailNormalized.Trim().ToLowerInvariant();

        var orderedRoots = IntentMelodyAliases.GetCatalogSnapshot()
            .Roots
            .Values
            .Where(e => e.Shape == IntentMelodyShape.Parametric && !IntentMelodyTailSemantics.HasUrlSlot(e.TailSignature))
            .OrderByDescending(e => e.Slug.Length);

        foreach (var e in orderedRoots)
        {
            if (string.IsNullOrWhiteSpace(e.WireClass)
                || !IntentMelodyCatalog.TryGetTailWireClass(e.WireClass, out var wire))
                continue;
            if (wire.Kind != TailWireKind.DelimitedSlots)
                continue;

            var expectedNumericSlots = IntentMelodyTailSemantics.CountDelimitedNumericSlots(e.TailSignature);
            if (expectedNumericSlots != 2)
                continue;

            if (!TailTryStripRemainderAfterSlug(tailNormalized, e, wire, true, out var aliasCanonical, out var displayTailCanon, out var remainderTrimmed))
                continue;

            if (!SegmentPrefixIsIncompleteOrFilled(wire, remainderTrimmed, expectedNumericSlots, requireFull: true)
                || !TryExtractLineRangeFromRemainder(wire, remainderTrimmed, out var lines))
                continue;

            parsed = new ParsedLineRange(aliasCanonical, displayTailCanon, lines);
            return true;
        }

        return false;
    }

    /// <summary>Подсказка палитры по slug или по <see cref="MelodyRootEntry.PaletteHintSlug"/> (например <c>wai-url</c> → корень <c>wai</c>).</summary>
    public static string BuildAliasUsageHint(string alias)
    {
        var k = (alias ?? "").Trim().ToLowerInvariant();
        if (TryGetRootForPaletteUsage(k, out var e) && !string.IsNullOrWhiteSpace(e.PaletteUsageHint))
            return e.PaletteUsageHint.Trim();

        return $"c:{k}:<start>:<end>";
    }

    /// <inheritdoc cref="BuildAliasUsageHint"/>
    public static string BuildAliasUsageCategory(string alias)
    {
        var k = (alias ?? "").Trim().ToLowerInvariant();
        if (TryGetRootForPaletteUsage(k, out var e) && !string.IsNullOrWhiteSpace(e.PaletteUsageCategory))
            return e.PaletteUsageCategory.Trim();

        return "Parametric melody";
    }

    private static bool TryGetRootForPaletteUsage(string keyNormalized, out MelodyRootEntry entry)
    {
        entry = default;
        if (string.IsNullOrEmpty(keyNormalized))
            return false;

        if (IntentMelodyCatalog.TryGetRoot(keyNormalized, out entry))
            return true;

        foreach (var r in IntentMelodyAliases.GetCatalogSnapshot().Roots.Values)
        {
            var hintSlug = r.PaletteHintSlug?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(hintSlug) && string.Equals(hintSlug, keyNormalized, StringComparison.Ordinal))
            {
                entry = r;
                return true;
            }
        }

        return false;
    }

    /// <summary>Сборка args для диапазона строк; реализация — <see cref="ParametricLineRangeArgsBuilder"/> (ADR 0109, без binder в TOML).</summary>
    public static bool TryBuildExecutionArgs(
        ParsedLineRange parsed,
        string? currentFilePath,
        string? editorText,
        out string commandId,
        out string argsJson,
        out string error) =>
        ParametricLineRangeArgsBuilder.TryBuild(parsed, currentFilePath, editorText, out commandId, out argsJson, out error);

    public static string? TryGetAliasPrefixBeforeColon(string tailNormalized)
    {
        if (string.IsNullOrWhiteSpace(tailNormalized))
            return null;

        var idx = tailNormalized.IndexOf(':');
        if (idx <= 0)
            return null;

        var alias = tailNormalized[..idx].Trim();
        return string.IsNullOrEmpty(alias) ? null : alias;
    }

    private static IEnumerable<MelodyRootEntry> EnumerateSingleRemainderUrlRoots() =>
        IntentMelodyAliases.GetCatalogSnapshot().Roots.Values
            .Where(e => e.Shape == IntentMelodyShape.Parametric && IntentMelodyTailSemantics.HasUrlSlot(e.TailSignature));

    private static string NormalizeChordCommit(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "enter" : raw.Trim().ToLowerInvariant();

    private static bool SeparatorCharAllowed(TailWireClassEntry wire, char c)
    {
        foreach (var s in wire.BetweenSlotsSeparators)
        {
            if (s.Length == 1 && s[0] == c)
                return true;
        }

        return false;
    }

    /// <returns>Строку alias:… для отображения: однозначное каноническое разделительное между слотами первое допустимое из <paramref name="wire"/>.</returns>
    private static bool TailTryStripRemainderAfterSlug(
        string tailLower,
        MelodyRootEntry e,
        TailWireClassEntry wire,
        bool preferColonDisplay,
        out string aliasCanon,
        out string displayTailCanon,
        out string remainderTrimmedOnly)
    {
        aliasCanon = e.Slug;
        displayTailCanon = "";
        remainderTrimmedOnly = "";

        if (!tailLower.StartsWith(e.Slug, StringComparison.Ordinal))
            return false;

        if (tailLower.Length == e.Slug.Length)
        {
            return true;
        }

        var sep = tailLower[e.Slug.Length];
        if (!SeparatorCharAllowed(wire, sep))
            return false;

        var rest = tailLower[(e.Slug.Length + 1)..];

        remainderTrimmedOnly = rest.Trim();
        displayTailCanon =
            ComposeDisplayCanonical(e.Slug, remainderTrimmedOnly, wire, preferColonDisplay);
        return true;

        static string ComposeDisplayCanonical(string slug, string remainder, TailWireClassEntry wire, bool preferColon)
        {
            if (string.IsNullOrEmpty(remainder))
                return slug;

            if (preferColon)
            {
                var norm = NormalizeDelimitedInts(remainder, wire, ':');
                return $"{slug}:{norm}";
            }

            return $"{slug}:{remainder}";
        }
    }

    private static readonly ConcurrentDictionary<string, Regex> SeparatorSplitRegexCache = new(StringComparer.Ordinal);

    private static Regex SeparatorSplitRegex(TailWireClassEntry wire)
    {
        var cacheKey = $"{wire.Id}:{string.Join(",", wire.BetweenSlotsSeparators)}";
        return SeparatorSplitRegexCache.GetOrAdd(
            cacheKey,
            _ =>
            {
                var w = wire;
                var escaped = w.BetweenSlotsSeparators
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(static s => Regex.Escape(s))
                    .Distinct()
                    .ToArray();
                var inner = string.Join("|", escaped);
                return new Regex($"({inner})+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            });
    }

    private static string NormalizeDelimitedInts(string remainderTrimmed, TailWireClassEntry wire, char canonBetween)
    {
        if (string.IsNullOrEmpty(remainderTrimmed))
            return "";

        foreach (var c in remainderTrimmed)
        {
            if (char.IsWhiteSpace(c) && SeparatorCharAllowed(wire, ' '))
            {
                var tmp = SeparatorSplitRegex(wire).Replace(remainderTrimmed, canonBetween.ToString());
                tmp = TrimEdges(tmp, canonBetween);
                return tmp;
            }

            if (SeparatorCharAllowed(wire, c) || char.IsDigit(c))
                continue;
            return remainderTrimmed;
        }

        return remainderTrimmed;
    }

    private static string TrimEdges(string s, char sepChar)
    {
        var span = s.AsSpan().Trim(sepChar);
        return span.Length == s.Length ? s : span.ToString();
    }

    private static bool SegmentPrefixIsIncompleteOrFilled(
        TailWireClassEntry wire,
        string remainderNoSlug,
        int expectedInts,
        bool requireFull)
    {
        remainderNoSlug ??= "";

        foreach (var c in remainderNoSlug)
        {
            if (char.IsWhiteSpace(c) && SeparatorCharAllowed(wire, ' '))
                break;
            if (SeparatorCharAllowed(wire, c) || char.IsDigit(c))
                continue;
            return false;
        }

        var norm = SeparatorSplitRegex(wire).Replace(remainderNoSlug.Trim(), ":");
        norm = TrimEdges(norm, ':');

        if (norm.Length == 0)
            return !requireFull;

        var sepIdx = norm.IndexOf(':');
        if (sepIdx < 0)
            return true;

        if (!AllDigits(norm.AsSpan(0, sepIdx)))
            return false;

        if (sepIdx + 1 >= norm.Length)
            return !requireFull;

        ReadOnlySpan<char> second = norm.AsSpan(sepIdx + 1);

        foreach (var c in second)
        {
            if (c == ':')
                return false;

            if (c is >= '0' and <= '9')
                continue;
            return false;
        }

        if (requireFull && second.Length == 0)
            return false;

        return !requireFull || second.Length > 0;
    }

    private static bool TryExtractLineRangeFromRemainder(
        TailWireClassEntry wire,
        string remainderNoSlug,
        out LineRange range)
    {
        range = default;

        foreach (var c in remainderNoSlug)
        {
            if (!(char.IsWhiteSpace(c) && SeparatorCharAllowed(wire, ' '))
                && !SeparatorCharAllowed(wire, c)
                && !char.IsDigit(c))
                return false;
        }

        var norm = SeparatorSplitRegex(wire).Replace(remainderNoSlug.Trim(), ":");
        norm = TrimEdges(norm, ':');

        var firstSep = norm.IndexOf(':');

        // Одно число — одна строка (эквивалент <start> == <end>).
        if (firstSep < 0)
        {
            if (!AllDigits(norm.AsSpan()))
                return false;

            if (!int.TryParse(norm, out var n) || !LineNumber.TryCreate(n, out var lone))
                return false;

            return LineRange.TryCreate(lone, lone, out range);
        }

        if (firstSep == 0)
            return false;

        var secondSep = norm.IndexOf(':', firstSep + 1);
        if (secondSep >= 0)
            return false;

        ReadOnlySpan<char> sa = norm.AsSpan(0, firstSep);
        ReadOnlySpan<char> sb = norm.AsSpan(firstSep + 1);

        if (!int.TryParse(sa, out var a) || !int.TryParse(sb, out var b))
            return false;

        // Два слота — границы одного инклюзивного диапазона; порядок ввода (3:7 vs 7:3) не важен.
        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);

        if (!LineNumber.TryCreate(lo, out var start) || !LineNumber.TryCreate(hi, out var end))
            return false;

        return LineRange.TryCreate(start, end, out range);
    }

    private static bool AllDigits(ReadOnlySpan<char> s)
    {
        foreach (var c in s)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return s.Length > 0;
    }
}

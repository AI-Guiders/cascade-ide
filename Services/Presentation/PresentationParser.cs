using System.Globalization;

namespace CascadeIDE.Services.Presentation;

/// <summary>Парсер строки <c>presentation</c> / <c>zone_screen_layout</c> (EBNF — ADR 0017). Литералы якорей задаются в TOML (<see cref="PresentationGrammarTokens"/>).</summary>
public static class PresentationParser
{
    /// <summary>Пустая или пробельная строка — пустой список экранов (снаружи: «не задано»).</summary>
    public static PresentationParseResult Parse(string? presentation, PresentationGrammarTokens grammar)
    {
        if (string.IsNullOrWhiteSpace(presentation))
            return PresentationParseResult.Ok(Array.Empty<IReadOnlyList<PresentationAnchorKind>>());

        var text = presentation.Trim();
        var screens = new List<IReadOnlyList<PresentationAnchorKind>>();
        var i = 0;
        SkipScreenSeparators(text, grammar.ScreenSeparator, ref i);

        while (i < text.Length)
        {
            if (text[i] != grammar.ScreenOpen)
            {
                return PresentationParseResult.Fail(
                    string.Format(CultureInfo.InvariantCulture, "Ожидался '{0}' начала экрана на позиции {1}.", grammar.ScreenOpen, i));
            }

            i++;
            var startInner = i;
            while (i < text.Length && text[i] != grammar.ScreenClose)
                i++;

            if (i >= text.Length)
                return PresentationParseResult.Fail($"Не найден закрывающий символ '{grammar.ScreenClose}' для экрана.");

            var inner = text.AsSpan(startInner, i - startInner);
            i++;

            var anchors = ParseAnchorsInner(inner, grammar);
            if (anchors.Error is { } err)
                return PresentationParseResult.Fail(err);

            screens.Add(anchors.Value);

            SkipScreenSeparators(text, grammar.ScreenSeparator, ref i);
        }

        return PresentationParseResult.Ok(screens);
    }

    private static void SkipScreenSeparators(string text, string separator, ref int i)
    {
        while (i < text.Length)
        {
            if (separator.Length > 0 && i + separator.Length <= text.Length && text.AsSpan(i).StartsWith(separator, StringComparison.Ordinal))
            {
                i += separator.Length;
                continue;
            }

            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            break;
        }
    }

    private static (List<PresentationAnchorKind> Value, string? Error) ParseAnchorsInner(ReadOnlySpan<char> inner, PresentationGrammarTokens grammar)
    {
        var list = new List<PresentationAnchorKind>();
        var s = inner.Trim();
        if (s.Length == 0)
            return (list, "Пустой список якорей внутри границ экрана.");

        var i = 0;
        while (i < s.Length)
        {
            if (list.Count > 0)
            {
                if (!TrySkipZoneSeparator(s, grammar, ref i))
                    return (list, $"Ожидался разделитель якорей на позиции {i}.");
            }

            if (!TryConsumeAnchorToken(s, grammar, ref i, out var kind))
                return (list, $"Неизвестный якорь на позиции {i}.");

            list.Add(kind);
        }

        return (list, null);
    }

    private static bool TrySkipZoneSeparator(ReadOnlySpan<char> s, PresentationGrammarTokens g, ref int i)
    {
        if (g.ZoneSeparator.Length > 0 && RemainingStartsWith(s, i, g.ZoneSeparator))
        {
            i += g.ZoneSeparator.Length;
            return true;
        }

        return false;
    }

    private static bool TryConsumeAnchorToken(ReadOnlySpan<char> s, PresentationGrammarTokens g, ref int i, out PresentationAnchorKind kind)
    {
        kind = default;
        var r = s.Slice(i);

        var pairs = new (string Token, PresentationAnchorKind K)[]
        {
            (g.PfdZoneIdentifier, PresentationAnchorKind.Pfd),
            (g.ForwardZoneIdentifier, PresentationAnchorKind.Forward),
            (g.MfdZoneIdentifier, PresentationAnchorKind.Mfd),
        };

        Array.Sort(pairs, static (a, b) => b.Token.Length.CompareTo(a.Token.Length));

        foreach (var (token, k) in pairs)
        {
            if (token.Length == 0)
                continue;

            if (token.Length == 1)
            {
                if (r.Length < 1)
                    continue;
                if (char.ToUpperInvariant(r[0]) == char.ToUpperInvariant(token[0]))
                {
                    i += 1;
                    kind = k;
                    return true;
                }

                continue;
            }

            if (r.StartsWith(token, StringComparison.Ordinal))
            {
                i += token.Length;
                kind = k;
                return true;
            }
        }

        return false;
    }

    private static bool RemainingStartsWith(ReadOnlySpan<char> s, int i, string sep)
    {
        if (sep.Length == 0 || i + sep.Length > s.Length)
            return false;
        return s.Slice(i, sep.Length).SequenceEqual(sep.AsSpan());
    }
}

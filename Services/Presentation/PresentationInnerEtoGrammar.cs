using System.Collections.Concurrent;
using Eto.Parse;
using Eto.Parse.Parsers;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Внутренняя грамматика одного экрана <c>( … )</c> через <see cref="Eto.Parse"/> (Fluent API, семантика как в EBNF ADR 0017).
/// Внешний уровень (несколько <c>(screen)</c>, разделители между экранами) — по-прежнему в <see cref="PresentationParser"/>.
/// </summary>
internal static class PresentationInnerEtoGrammar
{
    private static readonly ConcurrentDictionary<PresentationGrammarTokens, Grammar> Cache = new();

    /// <summary>Собирает грамматику «якорь с опциональным весом», повторённую через <see cref="PresentationGrammarTokens.ZoneSeparator"/>.</summary>
    public static Grammar GetOrCreate(PresentationGrammarTokens grammar)
    {
        return Cache.GetOrAdd(grammar, Build);
    }

    private static Grammar Build(PresentationGrammarTokens g)
    {
        var zoneSep = Terminals.Literal(g.ZoneSeparator);
        var anchor = BuildAnchorChoice(g).Named("anchor");

        var digit = Terminals.Digit;
        var intPart = digit.Repeat(1);
        var frac = Terminals.Set('.').Then(digit.Repeat(1));
        var weight = intPart.Then(frac.Optional()).Named("weight");

        var weighted = weight.Optional().Then(anchor).Named("slot");

        var inner = weighted.Then(new RepeatParser(zoneSep.Then(weighted), 0));

        var rule = inner.Then(Terminals.End);
        return new Grammar(rule)
        {
            CaseSensitive = true,
            EnableMatchEvents = false,
        };
    }

    /// <summary>Длинные литералы первыми, затем односимвольные (как <see cref="PresentationParser"/>).</summary>
    private static Parser BuildAnchorChoice(PresentationGrammarTokens g)
    {
        var pairs = new (string Token, PresentationAnchorKind K)[]
        {
            (g.PfdZoneIdentifier, PresentationAnchorKind.Pfd),
            (g.ForwardZoneIdentifier, PresentationAnchorKind.Forward),
            (g.MfdZoneIdentifier, PresentationAnchorKind.Mfd),
        };

        Array.Sort(pairs, static (a, b) => b.Token.Length.CompareTo(a.Token.Length));

        Parser? chain = null;
        foreach (var (token, _) in pairs)
        {
            if (token.Length == 0)
                continue;

            Parser p = token.Length == 1
                ? Terminals.Set(char.ToUpperInvariant(token[0]), char.ToLowerInvariant(token[0]))
                : Terminals.Literal(token);

            chain = chain is null ? p : chain.Or(p);
        }

        return chain ?? Terminals.Set('\uFFFF');
    }
}

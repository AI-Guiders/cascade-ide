using Eto.Parse;

namespace CascadeIDE.Services.ChordNotation;

/// <summary>
/// Грамматика Eto.Parse для нотации аккордов. Соответствует EBNF в <c>docs/chord-notation-cascadeide.md</c> (§ EBNF).
/// </summary>
internal static class ChordNotationGrammar
{
    private static readonly Lazy<Grammar> Lazy = new(Build);

    public static Grammar Instance => Lazy.Value;

    /// <summary>Сборка fluent-грамматики (эквивалент EBNF в документе).</summary>
    private static Grammar Build()
    {
        Parser mod(string s) => s;
        var modifier =
            mod("Alt-") | mod("C-") | mod("M-") | mod("A-") | mod("S-") | mod("D-");

        var key = (+Terminals.LetterOrDigit).Named("key");

        var bracketInner = modifier.Repeat(0) & key;

        var bracket = ("<" & bracketInner & ">").Named("bracket");

        var plain = (+Terminals.LetterOrDigit).Named("plain");

        var step = (bracket | plain).Named("step");

        var sp = +Terminals.WhiteSpace;

        var sequence = Terminals.WhiteSpace.Repeat(0) & step & (sp & step).Repeat(0) & Terminals.WhiteSpace.Repeat(0) & Terminals.End;

        return new Grammar("chord_sequence", sequence);
    }
}

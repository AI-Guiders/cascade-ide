#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Подсказка автокомплита внутри <c>[…]</c> (ADR 0128 §5).</summary>
public sealed record ChatBracketSuggestion(
    string Display,
    string Help,
    string? Group,
    string NewBracketInner,
    bool AddClosingBracket = true);

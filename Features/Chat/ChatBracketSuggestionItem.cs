#nullable enable

namespace CascadeIDE.Features.Chat;

public sealed class ChatBracketSuggestionItem(ChatBracketSuggestion suggestion, int bracketStart, int replaceEnd)
{
    public string Display { get; } = suggestion.Display;

    public string Help { get; } = suggestion.Help;

    public string? Group { get; } = suggestion.Group;

    public int BracketStart { get; } = bracketStart;

    public int ReplaceEnd { get; } = replaceEnd;

    public string NewBracketInner { get; } = suggestion.NewBracketInner;

    public bool AddClosingBracket { get; } = suggestion.AddClosingBracket;
}

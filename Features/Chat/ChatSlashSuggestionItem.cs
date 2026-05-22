#nullable enable

namespace CascadeIDE.Features.Chat;

public sealed class ChatSlashSuggestionItem(ChatSlashSuggestion suggestion)
{
    public string InsertText { get; } = suggestion.InsertText;
    public string SlashPath { get; } = suggestion.SlashPath;
    public string Help { get; } = suggestion.Help;
    public string? Group { get; } = suggestion.Group;

    /// <summary>Заголовок строки popup (следующий шаг Tab, не полный канонический путь).</summary>
    public string ListTitle { get; } = suggestion.ListTitle;

    /// <summary>Подзаголовок: канонический slash + help.</summary>
    public string ListSubtitle { get; } = suggestion.ListSubtitle;
}

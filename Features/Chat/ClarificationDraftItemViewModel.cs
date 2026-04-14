using System.Collections.ObjectModel;
using CascadeIDE.Models.AgentChat;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

/// <summary>Черновой ответ пользователя на один пункт пакета уточнений.</summary>
public sealed partial class ClarificationDraftItemViewModel : ObservableObject
{
    public ClarificationDraftItemViewModel(ClarificationItem item)
    {
        Id = item.Id;
        Prompt = item.Prompt;
        AnswerStyle = item.AnswerStyle;
        ChoiceOptions = BuildChoiceOptions(item);
    }

    public string Id { get; }

    public string Prompt { get; }

    public ClarificationAnswerStyle AnswerStyle { get; }

    public ObservableCollection<string> ChoiceOptions { get; }

    public bool IsFreeText => AnswerStyle == ClarificationAnswerStyle.FreeText;

    public bool IsChoice => AnswerStyle is ClarificationAnswerStyle.SingleChoice or ClarificationAnswerStyle.YesNo;

    [ObservableProperty]
    private string _answer = "";

    private static ObservableCollection<string> BuildChoiceOptions(ClarificationItem item)
    {
        if (item.AnswerStyle == ClarificationAnswerStyle.YesNo)
            return ["Да", "Нет"];

        var values = item.ChoiceOptions ?? [];
        return [.. values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())];
    }
}

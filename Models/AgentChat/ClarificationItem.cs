namespace CascadeIDE.Models.AgentChat;

/// <summary>ADR 0031: один пункт внутри пакета уточнений; <see cref="Id"/> стабилен в пределах пакета.</summary>
public sealed record ClarificationItem(
    string Id,
    string Prompt,
    ClarificationAnswerStyle AnswerStyle = ClarificationAnswerStyle.FreeText,
    IReadOnlyList<string>? ChoiceOptions = null);

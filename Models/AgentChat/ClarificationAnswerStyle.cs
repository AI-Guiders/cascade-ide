namespace CascadeIDE.Models.AgentChat;

/// <summary>ADR 0031: предполагаемый стиль ответа на пункт пакета уточнений (UI может игнорировать часть значений в v1).</summary>
public enum ClarificationAnswerStyle
{
    /// <summary>Свободный многострочный текст.</summary>
    FreeText,

    /// <summary>Да / нет (или эквивалент одной строкой).</summary>
    YesNo,

    /// <summary>Один вариант из <see cref="ClarificationItem.ChoiceOptions"/>.</summary>
    SingleChoice,
}

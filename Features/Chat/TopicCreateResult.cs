#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Результат <c>/topic create</c> / <c>/card</c> (без эвристик по тексту).</summary>
public readonly record struct TopicCreateResult(bool Success, string Message)
{
    public static TopicCreateResult Ok(string message) => new(true, message);
    public static TopicCreateResult Fail(string message) => new(false, message);
}

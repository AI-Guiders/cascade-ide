#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Результат <c>/topic rename</c> и UI переименования темы.</summary>
public readonly record struct TopicRenameResult(bool Success, string Message)
{
    public static TopicRenameResult Ok(string message) => new(true, message);
    public static TopicRenameResult Fail(string message) => new(false, message);
}

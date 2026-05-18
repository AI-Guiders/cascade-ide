#nullable enable

namespace CascadeIDE.Features.Chat;

public readonly record struct ChatSlashIntercomResult(bool Success, string Message)
{
    public static ChatSlashIntercomResult Ok(string message) => new(true, message);
    public static ChatSlashIntercomResult Fail(string message) => new(false, message);
}

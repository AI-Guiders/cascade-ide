namespace CascadeIDE.Features.Chat;

public static class ChatMessageVisualRoleMapping
{
    public static ChatMessageVisualRole FromMessageRole(string role) =>
        role switch
        {
            "user" => ChatMessageVisualRole.User,
            "assistant" => ChatMessageVisualRole.Assistant,
            "thinking" => ChatMessageVisualRole.Thinking,
            "tool" => ChatMessageVisualRole.Tool,
            "slash_command" => ChatMessageVisualRole.SlashCommand,
            _ => ChatMessageVisualRole.Assistant
        };
}

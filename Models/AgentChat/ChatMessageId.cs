#nullable enable

namespace CascadeIDE.Models;

/// <summary>Идентификатор сообщения чата (MCP <c>edit_chat_assistant_message</c> и UI).</summary>
public readonly record struct ChatMessageId(Guid Value)
{
    public static implicit operator Guid(ChatMessageId id) => id.Value;

    public static bool TryParse(string? s, out ChatMessageId id)
    {
        if (!Guid.TryParse(s, out var g))
        {
            id = default;
            return false;
        }

        id = new ChatMessageId(g);
        return true;
    }
}

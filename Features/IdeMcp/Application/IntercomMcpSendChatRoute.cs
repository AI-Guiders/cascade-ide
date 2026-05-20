using CascadeIDE.Contracts;
using CascadeIDE.Features.Chat.Application;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>Маршрут <c>send_chat</c>: fast append в ленту vs полная отправка через composer.</summary>
[ComputingUnit]
public static class IntercomMcpSendChatRoute
{
    public static bool ShouldAppendPreparedFeedMessage(string? role, string message) =>
        string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)
        || IntercomAttachSyntax.HasWireOrBracketSyntax(message);
}

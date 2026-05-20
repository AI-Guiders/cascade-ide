#nullable enable

using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>Единый commit подготовленного сообщения в ленту (ADR 0134).</summary>
public static class IntercomPreparedMessageCommit
{
    public static ChatMessageViewModel ToViewModel(
        string role,
        string displayBody,
        PreparedIntercomMessage prepared,
        Guid? threadId = null,
        Guid? parentMessageId = null) =>
        new(
            role,
            displayBody,
            threadId: threadId,
            parentMessageId: parentMessageId,
            attachments: prepared.Outbound.Attachments,
            senderWorkspaceContext: prepared.Outbound.SenderWorkspaceContext);

    public static string? FormatStatusHint(PreparedIntercomMessage prepared)
    {
        if (prepared.Status == IntercomMessagePrepareStatus.PartialSuccess && prepared.Warnings.Count > 0)
            return "Вложения: " + string.Join("; ", prepared.Warnings);

        return null;
    }
}

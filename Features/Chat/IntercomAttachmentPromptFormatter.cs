#nullable enable

using System.Text;
using System.Text.Json;
using CascadeIDE.Contracts;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Блок attachments для prompt агента (ADR 0128 §10).</summary>
[ComputingUnit]
public static class IntercomAttachmentPromptFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static string AppendToUserMessage(
        string userContent,
        IReadOnlyList<AttachmentAnchor> attachments,
        SenderWorkspaceContext? senderContext)
    {
        if (attachments.Count == 0 && senderContext is null)
            return userContent;

        var sb = new StringBuilder(userContent);
        if (senderContext is not null)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("--- sender_workspace_context ---");
            sb.AppendLine(JsonSerializer.Serialize(senderContext, JsonOptions));
        }

        if (attachments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- attachments ---");
            sb.AppendLine(JsonSerializer.Serialize(attachments, JsonOptions));
        }

        return sb.ToString();
    }
}

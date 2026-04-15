using System.Text;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>Семантический текстовый экспорт чата для агента/людей (Markdown).</summary>
public static class ChatReadableExporter
{
    public static string BuildMarkdown(Guid sessionId, IReadOnlyList<ChatMessageViewModel> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Chat session `{sessionId:N}`");
        sb.AppendLine();
        sb.AppendLine($"Messages: **{messages.Count}**");
        sb.AppendLine();

        for (var i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            var role = (m.Role ?? "unknown").Trim();
            sb.Append($"## [{i}] **{role}** · `message_id={m.MessageId:N}`");
            if (m.ThreadId != Guid.Empty)
                sb.Append($" · `thread_id={m.ThreadId:N}`");
            if (m.ParentMessageId is { } par)
                sb.Append($" · `parent_message_id={par:N}`");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(m.Content ?? "");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

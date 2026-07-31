#nullable enable
using System.Text.Json;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>Message/thread selection, thinking toggle, assistant edit, readable export.</summary>
public partial class ChatPanelViewModel
{
    public string SelectMessageByIndex(int index)
    {
        if (index < 0 || index >= ChatMessages.Count)
            return $"Index out of range: {index}. Count={ChatMessages.Count}.";
        HighlightedMessageIndices = new HashSet<int> { index };
        SelectedMessageIndex = index;
        RefreshChatSurfaceSnapshot();
        return "OK";
    }

    /// <summary>Сдвинуть выбор в ленте сообщений на delta (-1/ +1) для keyboard-first сценария.</summary>
    public string SelectMessageByOffset(int delta)
    {
        if (ChatMessages.Count == 0)
            return "No messages";
        var current = SelectedMessageIndex;
        if (current < 0)
            current = delta >= 0 ? 0 : ChatMessages.Count - 1;
        var next = Math.Clamp(current + delta, 0, ChatMessages.Count - 1);
        SelectedMessageIndex = next;
        return "OK";
    }

    /// <summary>Сдвинуть выбор темы в overview по циклу.</summary>
    public string NavigateThreadSelection(int delta)
    {
        var threads = ChatSurfaceSnapshot.Layout.Overview;
        if (threads.Count == 0)
            return "No threads";
        var current = -1;
        for (var i = 0; i < threads.Count; i++)
        {
            if (threads[i].ThreadId == SelectedChatThreadId)
            {
                current = i;
                break;
            }
        }
        if (current < 0)
        {
            for (var i = 0; i < threads.Count; i++)
            {
                if (!threads[i].IsActive)
                    continue;
                current = i;
                break;
            }
        }
        if (current < 0)
            current = 0;
        var next = (current + delta) % threads.Count;
        if (next < 0)
            next += threads.Count;
        SelectedChatThreadId = threads[next].ThreadId;
        return "OK";
    }

    public string OpenSelectedThreadDetail()
    {
        if (SelectedChatThreadId == Guid.Empty)
            return "No selected thread";
        IsChatOverviewMode = false;
        return "OK";
    }

    public string ShowThreadOverview()
    {
        IsChatOverviewMode = true;
        return "OK";
    }

    /// <summary>Переключить выбранный thinking-блок между свёрнутым и полным видом.</summary>
    public string ToggleSelectedThinkingDetails()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= ChatMessages.Count)
            return "No selected message";
        var selected = ChatMessages[SelectedMessageIndex];
        if (!string.Equals(selected.Role, "thinking", StringComparison.OrdinalIgnoreCase))
            return "Selected message is not thinking";
        if (!_collapsedThinkingByMessageId.TryGetValue(selected.MessageId, out var full))
            return "Thinking message has no stored details";

        if (selected.Content.StartsWith(CollapsedThinkingPrefix, StringComparison.Ordinal))
            selected.Content = full;
        else
            selected.Content = BuildCollapsedThinkingPreview(full);
        return "OK";
    }

    public string GetSelectedMessageJson()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= ChatMessages.Count)
            return "{\"selected_index\":-1,\"has_selection\":false}";
        var m = ChatMessages[SelectedMessageIndex];
        var role = m.Role ?? "";
        var content = m.Content ?? "";
        int? feedOrdinal = null;
        int? branchMessageCount = null;
        if (TryGetActiveDetailLaneMessageIndices(out var branchIndices))
        {
            branchMessageCount = branchIndices.Count;
            if (TryGetFeedOrdinalForMessageIndex(SelectedMessageIndex, out var ord))
                feedOrdinal = ord;
        }

        return JsonSerializer.Serialize(new
        {
            selected_index = SelectedMessageIndex,
            feed_ordinal = feedOrdinal,
            branch_message_count = branchMessageCount,
            has_selection = true,
            message_id = m.MessageId.ToString("N"),
            thread_id = m.ThreadId.ToString("N"),
            parent_message_id = m.ParentMessageId?.ToString("N"),
            role,
            content
        }, ChatPanelJson);
    }

    /// <summary>Редактирование только ответа ассистента; в лог добавляется <see cref="ChatHistoryEventKind.MessageEdited"/>.</summary>
    public string EditAssistantMessageById(Guid messageId, string newContent, string? reason)
    {
        foreach (var m in ChatMessages)
        {
            if (m.MessageId != messageId)
                continue;
            if (!string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Serialize(new { ok = false, error = "only_assistant_supported" }, ChatPanelJson);

            m.Content = newContent;
            _ = PersistEventAsync(
                ChatHistoryEventKind.MessageEdited,
                ChatHistoryPayloadMapping.ToMessageEditedPayload(messageId, newContent, reason));
            return JsonSerializer.Serialize(new { ok = true, message_id = messageId.ToString("N") }, ChatPanelJson);
        }

        return JsonSerializer.Serialize(new { ok = false, error = "message_not_found" }, ChatPanelJson);
    }

    /// <summary>Читаемый Markdown текущего чата; опционально запись в .cascade-ide/chat-sessions/exports/.</summary>
    public string ExportReadableMarkdown(bool writeFile, string? fileName)
    {
        var md = ChatReadableExporter.BuildMarkdown(_sessionId, [.. ChatMessages]);
        var decisions = ChatSedmReadableExport.BuildDecisionsSection(_sessionEventsCache);
        if (!string.IsNullOrWhiteSpace(decisions))
            md = md.TrimEnd() + Environment.NewLine + Environment.NewLine + decisions + Environment.NewLine;
        if (!writeFile)
            return JsonSerializer.Serialize(new { ok = true, markdown = md, relative_path = (string?)null }, ChatPanelJson);

        try
        {
            var ws = _getWorkspaceRoot().Trim();
            if (string.IsNullOrEmpty(ws))
                ws = Environment.CurrentDirectory;
            var dir = Path.Combine(ws, ".cascade-ide", "chat-sessions", "exports");
            Directory.CreateDirectory(dir);
            var name = string.IsNullOrWhiteSpace(fileName) ? $"session-{_sessionId:N}.readable.md" : fileName.Trim();
            if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                name += ".md";
            var safe = Path.GetFileName(name);
            var full = Path.Combine(dir, safe);
            File.WriteAllText(full, md, System.Text.Encoding.UTF8);
            var relative = Path.GetRelativePath(ws, full);
            return JsonSerializer.Serialize(new { ok = true, markdown = md, relative_path = relative }, ChatPanelJson);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { ok = false, markdown = md, error = ex.Message }, ChatPanelJson);
        }
    }
}

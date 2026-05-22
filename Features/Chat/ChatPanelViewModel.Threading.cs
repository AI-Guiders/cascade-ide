#nullable enable
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private readonly Dictionary<Guid, string> _threadDisplayTitles = new();
    private readonly List<ChatThreadForkRecord> _threadForks = [];

    private TopicPickerPresentation _topicPickerPresentation;

    public void SetTopicPickerPresentation(TopicPickerPresentation presentation)
    {
        _topicPickerPresentation = presentation;
        RefreshChatSurfaceSnapshot();
    }

    private IReadOnlyDictionary<Guid, string> BuildThreadDisplayTitles() => _threadDisplayTitles;

    private void ApplyThreadTitlesFromMetadata(ChatSessionMetadata meta)
    {
        _threadDisplayTitles.Clear();
        if (meta.ThreadTitles is null)
            return;

        foreach (var pair in meta.ThreadTitles)
        {
            if (!Guid.TryParse(pair.Key, out var threadId))
                continue;
            if (string.IsNullOrWhiteSpace(pair.Value))
                continue;
            _threadDisplayTitles[threadId] = pair.Value.Trim();
        }
    }

    private async Task PersistThreadTitlesAsync()
    {
        try
        {
            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var titles = _threadDisplayTitles.Count == 0
                ? null
                : _threadDisplayTitles.ToDictionary(static kv => kv.Key.ToString("N"), static kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            var updated = meta with
            {
                ThreadTitles = titles,
                SchemaVersion = Math.Max(meta.SchemaVersion, 2),
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
            await _sessionStore.SaveMetadataAsync(updated, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // best-effort
        }
    }

    /// <summary>Новая ветка: следующее user-сообщение получит <see cref="ChatMessageViewModel.ParentMessageId"/>, если задано.</summary>
    public string ForkThread(Guid? parentMessageId, string? displayTitle = null)
    {
        var previous = _activeThreadId;
        if (previous == Guid.Empty)
            previous = _mainThreadId;
        _activeThreadId = Guid.NewGuid();
        _pendingParentForNextMessage = parentMessageId;
        RecordThreadFork(_activeThreadId, previous, parentMessageId);
        ApplyDisplayTitleForThread(_activeThreadId, displayTitle);
        SelectedChatThreadId = _activeThreadId;
        _ = PersistEventAsync(
            ChatHistoryEventKind.ThreadForked,
            ChatHistoryPayloadMapping.ToThreadForkedPayload(_activeThreadId, previous, parentMessageId),
            envelopeThreadId: _activeThreadId);
        RefreshChatSurfaceSnapshot();
        return "OK";
    }

    /// <summary>Новая ветка с явным заголовком (slash <c>/topic create</c>, <c>/card</c>).</summary>
    public TopicCreateResult CreateTopicWithTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return TopicCreateResult.Fail("Укажи заголовок: /topic create <название>");

        var trimmed = title.Trim();
        var forkResult = ForkThread(parentMessageId: null, displayTitle: trimmed);
        if (!string.Equals(forkResult, "OK", StringComparison.Ordinal))
            return TopicCreateResult.Fail(forkResult);

        _topicPickerPresentation = TopicPickerPresentation.None;
        IsChatOverviewMode = false;
        RefreshChatSurfaceSnapshot();
        return TopicCreateResult.Ok($"Создана тема: {trimmed}");
    }

    /// <summary>Переименовать тему (slash <c>/topic rename</c>, Navigator, вкладка). Пустой <paramref name="threadId"/> — выбранная.</summary>
    public TopicRenameResult RenameTopicWithTitle(string? title, Guid? threadId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return TopicRenameResult.Fail("Укажи название: /topic rename <название>");

        var id = threadId is { } explicitId && explicitId != Guid.Empty
            ? explicitId
            : SelectedChatThreadId;
        if (id == Guid.Empty)
            id = _activeThreadId != Guid.Empty ? _activeThreadId : _mainThreadId;
        if (id == Guid.Empty)
            return TopicRenameResult.Fail("Нет активной темы для переименования.");

        if (!ChatSurfaceSnapshot.State.Threads.Any(t => t.ThreadId == id))
            return TopicRenameResult.Fail("Тема не найдена в сессии.");

        var trimmed = title.Trim();
        ApplyDisplayTitleForThread(id, trimmed);
        if (SelectedChatThreadId != id)
            SelectedChatThreadId = id;
        IsChatOverviewMode = false;
        RefreshChatSurfaceSnapshot();
        return TopicRenameResult.Ok($"Тема переименована: {trimmed}");
    }

    /// <summary>Заголовок темы из snapshot (для диалога переименования).</summary>
    public string? TryGetThreadTitleForRename(Guid threadId)
    {
        if (threadId == Guid.Empty)
            return null;

        foreach (var t in ChatSurfaceSnapshot.State.Threads)
        {
            if (t.ThreadId == threadId)
                return t.Title;
        }

        return null;
    }

    private void RecordThreadFork(Guid newThreadId, Guid previousThreadId, Guid? parentMessageId)
    {
        if (newThreadId == Guid.Empty)
            return;

        _threadForks.RemoveAll(f => f.NewThreadId == newThreadId);
        _threadForks.Add(new ChatThreadForkRecord(newThreadId, previousThreadId, parentMessageId));
    }

    private void ApplyDisplayTitleForThread(Guid threadId, string? displayTitle)
    {
        if (string.IsNullOrWhiteSpace(displayTitle))
        {
            ThreadBranchHint = $"Ветка {threadId:N}";
            return;
        }

        var trimmed = displayTitle.Trim();
        _threadDisplayTitles[threadId] = trimmed;
        ThreadBranchHint = trimmed;
        _ = PersistThreadTitlesAsync();
    }

    partial void OnSelectedChatThreadIdChanged(Guid value)
    {
        if (_topicPickerPresentation != TopicPickerPresentation.None)
            _topicPickerPresentation = TopicPickerPresentation.None;

        RefreshChatSurfaceSnapshot();
    }
}

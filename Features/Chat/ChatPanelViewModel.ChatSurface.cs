#nullable enable
namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private readonly ChatSurfaceCompositor _chatSurfaceCompositor = new();
    private int _lastOverviewThreadCount = -1;

    private void RefreshChatSurfaceSnapshot()
    {
        ChatSurfaceSnapshot = _chatSurfaceCompositor.Compose(new ChatSurfaceIntent(
            BuildConversationMessages(),
            _activeClarificationBatch,
            SelectedMessageIndex,
            _mainThreadId,
            _activeThreadId,
            ThreadBranchHint,
            BuildProductSpine()));

        var overview = ChatSurfaceSnapshot.Layout.Overview;
        if (overview.Count == 0)
        {
            SelectedChatThreadId = Guid.Empty;
            _lastOverviewThreadCount = 0;
            return;
        }

        ApplyAdaptiveOverviewDefault(overview.Count);

        if (SelectedChatThreadId == Guid.Empty || !overview.Any(x => x.ThreadId == SelectedChatThreadId))
        {
            var preferred = Guid.Empty;
            foreach (var thread in overview)
            {
                if (thread.ThreadId != _activeThreadId)
                    continue;
                preferred = thread.ThreadId;
                break;
            }

            SelectedChatThreadId = preferred != Guid.Empty ? preferred : overview[0].ThreadId;
        }
    }

    private void ApplyAdaptiveOverviewDefault(int threadCount) =>
        ChatTopicOverviewPolicy.ApplyAdaptiveDefault(
            threadCount,
            ref _lastOverviewThreadCount,
            value => IsChatOverviewMode = value,
            () => IsChatOverviewMode);

    private IReadOnlyList<ChatConversationMessage> BuildConversationMessages() =>
        ChatMessages
            .Select((message, index) => new ChatConversationMessage(
                message.MessageId,
                message.Role,
                message.Content,
                message.ThreadId,
                message.ParentMessageId,
                index))
            .ToList();
}
